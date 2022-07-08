using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     Abstract base class for system groups
/// </summary>
[PublicAPI]
public abstract class ASystemGroup : ASystem
{
    /// <summary>
    ///     Stores all systems executed by this <see cref="ASystemGroup" />
    /// </summary>
    protected readonly Dictionary<Identification, ASystem> Systems = new();

    /// <summary>
    /// Systems to execute in <see cref="PostExecuteMainThread"/>
    /// </summary>
    protected readonly Queue<ASystem> PostExecuteSystems = new();

    /// <summary>
    ///     Setup the system group
    /// </summary>
    public override void Setup(SystemManager systemManager)
    {
        if (World is null) return;

        var childSystemIDs = SystemManager.SystemsPerSystemGroup[Identification];

        //Iterate and filter all registered child systems
        //and add the remaining ones as to the system group and initialize them
        foreach (var systemId in childSystemIDs.Where(systemId =>
                     (SystemManager.SystemExecutionSide[systemId].HasFlag(GameType.Server) || !World.IsServerWorld) &&
                     (SystemManager.SystemExecutionSide[systemId].HasFlag(GameType.Client) || World.IsServerWorld)))
        {
            var systemToAdd = SystemManager.SystemCreateFunctions[systemId](World);
            Systems.Add(systemId, systemToAdd);
            SetupSystem(systemManager, systemToAdd);

            systemManager.SetSystemActive(systemId, true);
        }
    }

    /// <summary>
    /// <see cref="ASystem.Setup"/> wrapper for child systems
    /// This method is available to alter/extend system setup behavior
    /// </summary>
    /// <param name="systemManager">The system manager the system lives in</param>
    /// <param name="system">The system to setup</param>
    protected virtual void SetupSystem(SystemManager systemManager, ASystem system)
    {
        system.Setup(systemManager);
    }


    /// <inheritdoc />
    public override void Dispose()
    {
        foreach (var (_, system) in Systems) system.Dispose();
        Systems.Clear();
        PostExecuteSystems.Clear();

        base.Dispose();
    }

    /// <inheritdoc />
    protected override void Execute()
    {
    }

    /// <summary>
    /// <see cref="ASystem.PreExecuteMainThread"/> wrapper for child systems
    /// This method is available to alter/extend system pre-execution behavior
    /// </summary>
    /// <param name="system">System to pre-execute</param>
    protected virtual void PreExecuteSystem(ASystem system)
    {
        system.PreExecuteMainThread();
    }

    /// <summary>
    /// <see cref="ASystem.PostExecuteMainThread"/> wrapper for child systems
    /// This method is available to alter/extend system post-execution behavior
    /// </summary>
    /// <param name="system">System to post-execute</param>
    protected virtual void PostExecuteSystem(ASystem system)
    {
        system.PostExecuteMainThread();
    }


    /// <inheritdoc />
    public override void PostExecuteMainThread()
    {
        while (PostExecuteSystems.TryDequeue(out var system))
        {
            PostExecuteSystem(system);
        }
    }

    /// <inheritdoc />
    public override Task QueueSystem(IEnumerable<Task> dependency)
    {
        if (World is null) return Task.CompletedTask;

        //This Method is mostly mirrored in SystemManager.Execute
        //If you make changes here make sure to adjust the other method as well

        //Collection of all created tasks
        List<Task> systemTaskCollection = new();

        var systemsToProcess = new Dictionary<Identification, ASystem>(Systems);

        //Dictionary to save the task of each queued system
        var systemTasks = systemsToProcess.Keys.ToDictionary(systemId => systemId, _ => Task.CompletedTask);

        var dependencyArray = dependency as Task[] ?? dependency.ToArray();

        while (systemsToProcess.Count > 0)
        {
            var systemsCopy = new Dictionary<Identification, ASystem>(systemsToProcess);

            foreach (var (id, system) in systemsCopy)
            {
                //Check if system is active
                if (!World.SystemManager.ActiveSystems.Contains(id))
                {
                    systemsToProcess.Remove(id);
                    continue;
                }

                //Check if all required systems are executed
                var missingDependency = SystemManager.ExecuteSystemAfter[id]
                    .Any(systemDepsId => systemsToProcess.ContainsKey(systemDepsId));

                if (missingDependency) continue;

                //Collect all needed dependency tasks for queueing the current system

                //First get the tasks of the systems which writes to components where the current system needs to read from (Multiple read accesses allowed or one write access)
                var systemDependency = (from component in SystemManager.SystemReadComponents[id]
                    where World.SystemManager.SystemComponentAccess[component].accessType == ComponentAccessType.Write
                    select World.SystemManager.SystemComponentAccess[component].task).ToList();

                //Second, get the tasks of the systems which uses the component which the current system needs to write to
                systemDependency.AddRange(SystemManager.SystemWriteComponents[id]
                    .Select(component => World.SystemManager.SystemComponentAccess[component].task));

                //Third, get the tasks of the systems which needs to be executed before the current system
                systemDependency.AddRange(SystemManager.ExecuteSystemAfter[id]
                    .Select(systemDepsId => systemTasks[systemDepsId]));

                systemDependency.AddRange(dependencyArray);

                //Start the system execution and save its task
                PreExecuteSystem(system);
                var systemTask = system.QueueSystem(systemDependency);
                systemTaskCollection.Add(systemTask);
                systemTasks[id] = systemTask;

                //Write the read component accesses of the current system to the combined task (if currently only reading tasks are present), or replace the current task if its a write access
                foreach (var component in SystemManager.SystemReadComponents[id])
                {
                    if (World.SystemManager.SystemComponentAccess[component].accessType == ComponentAccessType.Read)
                    {
                        var (accessType, task) = World.SystemManager.SystemComponentAccess[component];
                        World.SystemManager.SystemComponentAccess[component] =
                            (accessType, Task.WhenAll(task, systemTask));
                        continue;
                    }

                    (ComponentAccessType, Task) componentAccess = new(ComponentAccessType.Read, systemTask);
                    World.SystemManager.SystemComponentAccess[component] = componentAccess;
                }

                //Write the write component accesses tasks of the current system
                foreach (var component in SystemManager.SystemWriteComponents[id])
                {
                    (ComponentAccessType, Task) componentAccess = new(ComponentAccessType.Write, systemTask);
                    World.SystemManager.SystemComponentAccess[component] = componentAccess;
                }

                //"Mark" the system as processed
                systemsToProcess.Remove(id);
                PostExecuteSystems.Enqueue(system);
            }
        }

        return Task.WhenAll(systemTaskCollection);
    }
}