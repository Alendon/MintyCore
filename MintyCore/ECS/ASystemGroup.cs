using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     Abstract base class for system groups
/// </summary>
public abstract class ASystemGroup : ASystem
{
    /// <summary>
    ///     Stores all systems executed by this <see cref="ASystemGroup" />
    /// </summary>
    protected Dictionary<Identification, ASystem> Systems = new();

    protected Queue<ASystem> PostExecuteSystems = new();

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
                     (SystemManager.SystemExecutionSide[systemId].HasFlag(GameType.SERVER) || !World.IsServerWorld) &&
                     (SystemManager.SystemExecutionSide[systemId].HasFlag(GameType.CLIENT) || World.IsServerWorld)))
        {
            var systemToAdd = SystemManager.SystemCreateFunctions[systemId](World);
            Systems.Add(systemId, systemToAdd);
            systemToAdd.Setup(systemManager);

            systemManager.SetSystemActive(systemId, true);
        }
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


    /// <inheritdoc />
    public override void PostExecuteMainThread()
    {
        while (PostExecuteSystems.TryDequeue(out var system)) system.PostExecuteMainThread();
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
                    where World.SystemManager.SystemComponentAccess[component].accessType == ComponentAccessType.WRITE
                    select World.SystemManager.SystemComponentAccess[component].task).ToList();

                //Second, get the tasks of the systems which uses the component which the current system needs to write to
                systemDependency.AddRange(SystemManager.SystemWriteComponents[id]
                    .Select(component => World.SystemManager.SystemComponentAccess[component].task));

                //Third, get the tasks of the systems which needs to be executed before the current system
                systemDependency.AddRange(SystemManager.ExecuteSystemAfter[id]
                    .Select(systemDepsId => systemTasks[systemDepsId]));

                //Start the system execution and save its task
                system.PreExecuteMainThread();
                var systemTask = system.QueueSystem(systemDependency);
                systemTaskCollection.Add(systemTask);
                systemTasks[id] = systemTask;

                //Write the read component accesses of the current system to the combined task (if currently only reading tasks are present), or replace the current task if its a write access
                foreach (var component in SystemManager.SystemReadComponents[id])
                {
                    if (World.SystemManager.SystemComponentAccess[component].accessType == ComponentAccessType.READ)
                    {
                        var (accessType, task) = World.SystemManager.SystemComponentAccess[component];
                        World.SystemManager.SystemComponentAccess[component] =
                            (accessType, Task.WhenAll(task, systemTask));
                        continue;
                    }

                    (ComponentAccessType, Task) componentAccess = new(ComponentAccessType.READ, systemTask);
                    World.SystemManager.SystemComponentAccess[component] = componentAccess;
                }

                //Write the write component accesses tasks of the current system
                foreach (var component in SystemManager.SystemWriteComponents[id])
                {
                    (ComponentAccessType, Task) componentAccess = new(ComponentAccessType.WRITE, systemTask);
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