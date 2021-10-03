using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS
{
    /// <summary>
    /// Abstract base class for system groups
    /// </summary>
    public abstract class ASystemGroup : ASystem
    {
        /// <summary>
        /// Stores all systems executed by this <see cref="ASystemGroup"/>
        /// </summary>
        protected Dictionary<Identification, ASystem> Systems = new();

        /// <summary>
        /// Setup the system group
        /// </summary>
        public override void Setup()
        {
            if (World is null) return;
            
            var childSystemIDs = SystemManager.SystemsPerSystemGroup[Identification];

            foreach (var systemId in childSystemIDs)
            {
                if (!SystemManager.SystemExecutionSide[systemId].HasFlag(GameType.SERVER) && World.IsServerWorld || !SystemManager.SystemExecutionSide[systemId].HasFlag(GameType.CLIENT) && !World.IsServerWorld) continue;

                
                Systems.Add(systemId, SystemManager.SystemCreateFunctions[systemId](World));
                Systems[systemId].Setup();
            }
        }


        /// <inheritdoc />
        public override void Dispose()
        {
        }

        /// <inheritdoc />
        protected override void Execute()
        {
        }


        /// <inheritdoc />
        public override void PostExecuteMainThread()
        {
            foreach (var system in Systems) system.Value.PostExecuteMainThread();
        }

        /// <inheritdoc />
        public override Task QueueSystem(IEnumerable<Task> dependency)
        {
            if (World is null) return Task.CompletedTask;

            List<Task> systemTaskCollection = new();

            var systemsToProcess = new Dictionary<Identification, ASystem>(Systems);
            var systemTasks = systemsToProcess.Keys.ToDictionary(systemId => systemId, _ => Task.CompletedTask);

            while (systemsToProcess.Count > 0)
            {
                var systemsCopy = new Dictionary<Identification, ASystem>(systemsToProcess);

                foreach (var systemWithId in systemsCopy)
                {
                    var id = systemWithId.Key;
                    var system = systemWithId.Value;

                    //Check if system is active
                    if (World.SystemManager.InactiveSystems.Contains(id))
                    {
                        systemsToProcess.Remove(id);
                        continue;
                    }

                    //Check if all required systems are executed
                    var missingDependency = false;
                    foreach (var systemDepsId in SystemManager.ExecuteSystemAfter[id])
                        if (systemsToProcess.ContainsKey(systemDepsId))
                        {
                            missingDependency = true;
                            break;
                        }

                    if (missingDependency) continue;


                    List<Task> systemDependency = new();
                    //Collect all needed JobHandles for the systemDependency
                    foreach (var component in SystemManager.SystemReadComponents[id])
                        if (World.SystemManager.SystemComponentAccess[component].accessType ==
                            ComponentAccessType.WRITE)
                            systemDependency.Add(World.SystemManager.SystemComponentAccess[component].task);
                    foreach (var component in SystemManager.SystemWriteComponents[id])
                        systemDependency.Add(World.SystemManager.SystemComponentAccess[component].task);
                    foreach (var systemDepsId in SystemManager.ExecuteSystemAfter[id])
                        systemDependency.Add(systemTasks[systemDepsId]);

                    system.PreExecuteMainThread();
                    var systemTask = system.QueueSystem(systemDependency);
                    systemTaskCollection.Add(systemTask);
                    systemTasks[id] = systemTask;

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

                    foreach (var component in SystemManager.SystemWriteComponents[id])
                    {
                        (ComponentAccessType, Task) componentAccess = new(ComponentAccessType.WRITE, systemTask);
                        World.SystemManager.SystemComponentAccess[component] = componentAccess;
                    }

                    systemsToProcess.Remove(id);
                }
            }

            return Task.WhenAll(systemTaskCollection);
        }
    }
}