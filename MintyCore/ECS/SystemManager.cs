using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.ECS
{
	/// <summary>
	///     Specify that a system will be executed after one or multiple others
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
    public class ExecuteAfterAttribute : Attribute
    {
	    /// <summary>
	    ///     Specify that the system will be executed after <paramref name="executeAfter" />
	    /// </summary>
	    public ExecuteAfterAttribute(params Type[] executeAfter)
        {
            foreach (var type in executeAfter)
                if (Activator.CreateInstance(type) is not ASystem)
                    throw new ArgumentException(
                        "Types used with the ExecuteAfterAttribute have to be Assignable from ASystem");
            ExecuteAfter = executeAfter;
        }

        internal Type[] ExecuteAfter { get; }
    }

	/// <summary>
	///     Specify that a system will be executed before one or multiple others
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
    public class ExecuteBeforeAttribute : Attribute
    {
	    /// <summary>
	    ///     Specify that the system will be executed after <paramref name="executeBefore" />
	    /// </summary>
	    public ExecuteBeforeAttribute(params Type[] executeBefore)
        {
            foreach (var type in executeBefore)
                if (Activator.CreateInstance(type) is not ASystem)
                    throw new ArgumentException(
                        "Types used with the ExecuteBeforeAttribute have to be Assignable from ASystem");
            ExecuteBefore = executeBefore;
        }

        internal Type[] ExecuteBefore { get; }
    }

	/// <summary>
	///     Specify the SystemGroup the system will be executed in. If the attribute is not applied, the system will be
	///     executed in <see cref="SimulationSystemGroup" />
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
    public class ExecuteInSystemGroupAttribute : Attribute
    {
	    /// <summary />
	    public ExecuteInSystemGroupAttribute(Type systemGroup)
        {
            if (Activator.CreateInstance(systemGroup) is not ASystemGroup)
                throw new ArgumentException(
                    "Type used with the SystemGroupAttribute have to be Assignable from ASystem");
            SystemGroup = systemGroup;
        }

        internal Type SystemGroup { get; }
    }

	/// <summary>
	///     Specify that this SystemGroup is a RootSystemGroup (this system group does not have a parent system group)
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
    public class RootSystemGroupAttribute : Attribute
    {
    }

	/// <summary>
	///     Specify the ExecutionSide of a system
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
    public class ExecutionSideAttribute : Attribute
    {
	    /// <summary>
	    /// </summary>
	    public GameType ExecutionSide;

	    /// <summary>
	    ///     Specify the ExecutionSide of a system
	    /// </summary>
	    public ExecutionSideAttribute(GameType executionSide)
        {
            ExecutionSide = executionSide;
        }
    }

	/// <summary>
	///     The <see cref="SystemManager" /> contains all system handling stuff (populated by
	///     <see cref="Registries.SystemRegistry" /> and manages the systems for a <see cref="World" />
	/// </summary>
	public class SystemManager
    {
        internal HashSet<Identification> InactiveSystems = new();

        internal World Parent;
        internal Dictionary<Identification, ASystem> RootSystems = new();

        internal Dictionary<Identification, (ComponentAccessType accessType, Task task)> SystemComponentAccess = new();

        /// <summary>
        ///     Create a new SystemManager for <paramref name="world" />
        /// </summary>
        /// <param name="world"></param>
        public SystemManager(World world)
        {
            Parent = world;

            foreach (var systemId in RootSystemGroupIDs)
            {
                //Check if the world has active rendering
                if (systemId == SystemGroupIDs.Presentation && !world.IsRenderWorld) continue;

                RootSystems.Add(systemId, SystemCreateFunctions[systemId](Parent));
                RootSystems[systemId].Setup();
            }
        }

        internal void Execute()
        {
            RePopulateSystemComponentAccess();

            List<Task> systemTaskCollection = new();

            var rootSystemsToProcess = new Dictionary<Identification, ASystem>(RootSystems);
            var systemJobHandles = new Dictionary<Identification, Task>();

            while (rootSystemsToProcess.Count > 0)
            {
                var systemsCopy = new Dictionary<Identification, ASystem>(rootSystemsToProcess);

                foreach (var systemWithId in systemsCopy)
                {
                    var id = systemWithId.Key;
                    var system = systemWithId.Value;

                    //Check if system is active
                    if (InactiveSystems.Contains(id))
                    {
                        rootSystemsToProcess.Remove(id);
                        continue;
                    }

                    //Check if all required systems are executed
                    var missingDependency = false;
                    foreach (var dependency in ExecuteSystemAfter[id])
                        if (rootSystemsToProcess.ContainsKey(dependency))
                        {
                            missingDependency = true;
                            break;
                        }

                    if (missingDependency) continue;


                    List<Task> systemDependency = new();
                    //Collect all needed JobHandles for the systemDependency
                    foreach (var component in SystemReadComponents[id])
                        if (SystemComponentAccess[component].accessType == ComponentAccessType.WRITE)
                            systemDependency.Add(SystemComponentAccess[component].task);
                    foreach (var component in SystemWriteComponents[id])
                        systemDependency.Add(SystemComponentAccess[component].task);
                    foreach (var dependency in ExecuteSystemAfter[id])
                        systemDependency.Add(systemJobHandles[dependency]);

                    {
                        system.PreExecuteMainThread();
                    }

                    var systemTask = system.QueueSystem(systemDependency);
                    systemTaskCollection.Add(systemTask);
                    systemJobHandles[id] = systemTask;

                    foreach (var component in SystemReadComponents[id])
                    {
                        if (SystemComponentAccess[component].accessType == ComponentAccessType.READ)
                        {
                            var (accessType, task) = SystemComponentAccess[component];
                            SystemComponentAccess[component] = (accessType, Task.WhenAll(task, systemTask));
                            continue;
                        }

                        (ComponentAccessType, Task) componentAccess = new(ComponentAccessType.READ, systemTask);
                        SystemComponentAccess[component] = componentAccess;
                    }

                    foreach (var component in SystemWriteComponents[id])
                    {
                        (ComponentAccessType, Task) componentAccess = new(ComponentAccessType.WRITE, systemTask);
                        SystemComponentAccess[component] = componentAccess;
                    }

                    rootSystemsToProcess.Remove(id);
                }
            }


            //Wait for the completion of all systems
            Task.WhenAll(systemTaskCollection).Wait();

            foreach (var system in RootSystems) system.Value.PostExecuteMainThread();
        }

        private void RePopulateSystemComponentAccess()
        {
            foreach (var component in ComponentManager.GetComponentList())
            {
                if (!SystemComponentAccess.ContainsKey(component))
                {
                    SystemComponentAccess.Add(component,
                        new ValueTuple<ComponentAccessType, Task>(ComponentAccessType.NONE, Task.CompletedTask));
                    continue;
                }

                SystemComponentAccess[component] =
                    new ValueTuple<ComponentAccessType, Task>(ComponentAccessType.NONE, Task.CompletedTask);
            }
        }

        internal void ExecuteFinalization()
        {
            if (RootSystems.TryGetValue(SystemGroupIDs.Finalization, out var system))
            {
                RePopulateSystemComponentAccess();
                system.PreExecuteMainThread();
                Task.WhenAll(system.QueueSystem(Array.Empty<Task>())).Wait();
                system.PostExecuteMainThread();
            }
        }

        #region static setup stuff

        internal static Dictionary<Identification, Func<World, ASystem>> SystemCreateFunctions = new();

        internal static Dictionary<Identification, HashSet<Identification>> SystemReadComponents = new();
        internal static Dictionary<Identification, HashSet<Identification>> SystemWriteComponents = new();

        internal static HashSet<Identification> RootSystemGroupIDs = new();
        internal static Dictionary<Identification, HashSet<Identification>> SystemsPerSystemGroup = new();
        internal static Dictionary<Identification, HashSet<Identification>> ExecuteSystemAfter = new();
        internal static Dictionary<Identification, GameType> SystemExecutionSide = new();

        internal static HashSet<Identification> SystemsToSort = new();
        internal static Dictionary<Identification, Identification> SystemGroupPerSystem = new();

        internal static void Clear()
        {
            SystemCreateFunctions.Clear();
            SystemReadComponents.Clear();
            SystemWriteComponents.Clear();
            RootSystemGroupIDs.Clear();
            SystemsPerSystemGroup.Clear();
            ExecuteSystemAfter.Clear();
            SystemExecutionSide.Clear();
            SystemsToSort.Clear();
            SystemGroupPerSystem.Clear();
        }

        internal static void SetReadComponents(Identification systemId, HashSet<Identification> readComponents)
        {
            SystemReadComponents[systemId].UnionWith(readComponents);

            //Check if the current system is a root system group
            if (!RootSystemGroupIDs.Contains(systemId))
                //Recursive call with the parent SystemGroup
                SetReadComponents(SystemGroupPerSystem[systemId], readComponents);
        }

        internal static void SetWriteComponents(Identification systemId, HashSet<Identification> writeComponents)
        {
            SystemWriteComponents[systemId].UnionWith(writeComponents);

            //Check if the current system is a root system group
            if (!RootSystemGroupIDs.Contains(systemId))
                //Recursive call with the parent SystemGroup
                SetWriteComponents(SystemGroupPerSystem[systemId], writeComponents);
        }

        internal static void RegisterSystem<TSystem>(Identification systemId) where TSystem : ASystem, new()
        {
            SystemCreateFunctions.Add(systemId, world =>
            {
                var system = new TSystem();
                system.World = world;
                return system;
            });
            SystemsToSort.Add(systemId);

            SystemWriteComponents.Add(systemId, new HashSet<Identification>());
            SystemReadComponents.Add(systemId, new HashSet<Identification>());
            ExecuteSystemAfter.Add(systemId, new HashSet<Identification>());
        }

        private static void ValidateExecuteAfter(Identification systemId, Identification afterSystemId)
        {
            var isSystemRoot = RootSystemGroupIDs.Contains(systemId);
            var isToExecuteAfterRoot = RootSystemGroupIDs.Contains(afterSystemId);

            if (isSystemRoot && isToExecuteAfterRoot) return;

            if (isSystemRoot != isToExecuteAfterRoot)
                throw new Exception(
                    "Systems to execute after have to be either in the same group or be both a root system group");

            if (SystemGroupPerSystem[afterSystemId] != SystemGroupPerSystem[systemId])
                throw new Exception(
                    "Systems to execute after have to be either in the same group or be both a root system group");
        }

        private static void ValidateExecuteBefore(Identification systemId, Identification beforeSystemId)
        {
            var isSystemRoot = RootSystemGroupIDs.Contains(systemId);
            var isToExecuteBeforeRoot = RootSystemGroupIDs.Contains(beforeSystemId);

            if (isSystemRoot && isToExecuteBeforeRoot) return;

            if (isSystemRoot != isToExecuteBeforeRoot)
                throw new Exception(
                    "Systems to execute before have to be either in the same group or be both a root system group");

            if (SystemGroupPerSystem[beforeSystemId] != SystemGroupPerSystem[systemId])
                throw new Exception(
                    "Systems to execute before have to be either in the same group or be both a root system group");
        }

        internal static void SortSystems()
        {
            var systemInstances = new Dictionary<Identification, ASystem>();
            var systemTypes = new Dictionary<Identification, Type>();
            var reversedSystemTypes = new Dictionary<Type, Identification>();

            //Populate helper dictionaries
            foreach (var systemId in SystemsToSort)
            {
                var system = SystemCreateFunctions[systemId](null);
                var systemType = system.GetType();

                systemInstances.Add(systemId, system);
                systemTypes.Add(systemId, systemType);
                reversedSystemTypes.Add(systemType, systemId);
            }

            //Detect SystemGroups
            var rootSystemGroupType = typeof(RootSystemGroupAttribute);
            foreach (var systemId in SystemsToSort)
            {
                if (systemInstances[systemId] is not ASystemGroup) continue;

                if (Attribute.IsDefined(systemTypes[systemId], rootSystemGroupType)) RootSystemGroupIDs.Add(systemId);

                SystemsPerSystemGroup.Add(systemId, new HashSet<Identification>());
            }

            //Sort systems into SystemGroups
            var executeInSystemGroupType = typeof(ExecuteInSystemGroupAttribute);

            foreach (var systemId in SystemsToSort)
            {
                if (RootSystemGroupIDs.Contains(systemId)) continue;


                if (Attribute.GetCustomAttribute(systemTypes[systemId], executeInSystemGroupType) is not
                    ExecuteInSystemGroupAttribute executeInSystemGroup)
                {
                    SystemsPerSystemGroup[SystemGroupIDs.Simulation].Add(systemId);
                    SystemGroupPerSystem.Add(systemId, SystemGroupIDs.Simulation);
                    continue;
                }

                var systemGroupId = reversedSystemTypes[executeInSystemGroup.SystemGroup];

                SystemsPerSystemGroup[systemGroupId].Add(systemId);
                SystemGroupPerSystem.Add(systemId, systemGroupId);
            }

            //Sort execution order
            var executeAfterType = typeof(ExecuteAfterAttribute);
            var executeBeforeType = typeof(ExecuteBeforeAttribute);
            foreach (var systemId in SystemsToSort)
            {
                var executeAfter =
                    Attribute.GetCustomAttribute(systemTypes[systemId], executeAfterType) as ExecuteAfterAttribute;
                var executeBefore =
                    Attribute.GetCustomAttribute(systemTypes[systemId], executeBeforeType) as ExecuteBeforeAttribute;

                if (executeAfter is not null)
                {
                    if (!ExecuteSystemAfter.ContainsKey(systemId))
                        ExecuteSystemAfter.Add(systemId, new HashSet<Identification>());

                    foreach (var afterSystemType in executeAfter.ExecuteAfter)
                    {
                        if (!reversedSystemTypes.ContainsKey(afterSystemType))
                            throw new Exception("The system to execute after is not present");
                        var afterSystemId = reversedSystemTypes[afterSystemType];

                        ValidateExecuteAfter(systemId, afterSystemId);

                        ExecuteSystemAfter[systemId].Add(afterSystemId);
                    }
                }

                if (executeBefore is not null)
                    foreach (var beforeSystemType in executeBefore.ExecuteBefore)
                    {
                        if (!reversedSystemTypes.ContainsKey(beforeSystemType))
                            throw new Exception("The system to execute before is not present");
                        var beforeSystemId = reversedSystemTypes[beforeSystemType];

                        ValidateExecuteBefore(systemId, beforeSystemId);

                        ExecuteSystemAfter[beforeSystemId].Add(systemId);
                    }
            }

            //Sort execution side (client, server, both)
            var executionSideType = typeof(ExecutionSideAttribute);
            foreach (var systemId in SystemsToSort)
            {
                if (Attribute.GetCustomAttribute(systemTypes[systemId], executionSideType) is not ExecutionSideAttribute
                    executionSide)
                {
                    SystemExecutionSide.Add(systemId, GameType.LOCAL);
                    continue;
                }

                SystemExecutionSide.Add(systemId, executionSide.ExecutionSide);
            }

            SystemsToSort.Clear();
        }

        #endregion
    }

    internal enum ComponentAccessType
    {
        NONE = 0,
        READ,
        WRITE
    }
}