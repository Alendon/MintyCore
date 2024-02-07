using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.ECS;

//TODO Add multiple overloads of the ExecuteAfter and ExecuteBefore Attribute with multiple systems

/// <summary>
///     Specify that a system will be executed after one or multiple others
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExecuteAfterAttribute<[PublicAPI] TSystem> : Attribute where TSystem : ASystem
{
}

/// <summary>
///     Specify that a system will be executed before one or multiple others
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExecuteBeforeAttribute<[PublicAPI] TSystem> : Attribute where TSystem : ASystem
{
}

/// <summary>
///     Specify the SystemGroup the system will be executed in. If the attribute is not applied, the system will be
///     executed in <see cref="SimulationSystemGroup" />
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ExecuteInSystemGroupAttribute<[PublicAPI] TSystemGroup> : Attribute where TSystemGroup : ASystemGroup
{
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
    public readonly GameType ExecutionSide;

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
///     <see cref="Registries.SystemRegistry" /> and manages the systems for a <see cref="IWorld" />
/// </summary>
[PublicAPI]
public sealed class SystemManager : IDisposable
{
    internal readonly HashSet<Identification> ActiveSystems = new();

    internal IWorld Parent => _parent ?? throw new Exception("Object is Disposed");
    private ILifetimeScope SystemLifetimeScope;

    /// <summary>
    ///     Stores how a component (key) is accessed and the task by the system(s) which is using it
    /// </summary>
    internal readonly ConcurrentDictionary<Identification, (ComponentAccessType accessType, Task task)>
        SystemComponentAccess =
            new();

    /// <summary>
    ///     Root systems of this manager instance. Those are commonly system groups which contains other systems
    /// </summary>
    internal readonly Dictionary<Identification, ASystem> RootSystems = new();

    private IComponentManager ComponentManager { get; }

    /// <summary>
    ///     Create a new SystemManager for <paramref name="world" />
    /// </summary>
    /// <param name="world"></param>
    public SystemManager(IWorld world, IComponentManager componentManager, ILifetimeScope scope)
    {
        _parent = world;
        ComponentManager = componentManager;
        SystemLifetimeScope = CreateSystemLifetimeScope(scope);

        //Iterate and filter all registered root systems
        //and add the remaining ones as to the system group and initialize them
        foreach (var systemId in RootSystemGroupIDs.Where(systemId =>
                     (SystemExecutionSide[systemId].HasFlag(GameType.Server) || !world.IsServerWorld) &&
                     (SystemExecutionSide[systemId].HasFlag(GameType.Client) || world.IsServerWorld)))
        {
            var systemToAdd = SystemLifetimeScope.ResolveKeyed<ASystem>(systemId);
            systemToAdd.World = world;

            RootSystems.Add(systemId, systemToAdd);
            systemToAdd.Setup(this);
            SetSystemActive(systemId, true);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        RootSystems.Clear();
        
        SystemLifetimeScope.Dispose();
        
        _parent = null;
    }

    private readonly Queue<ASystem> _postExecuteSystems = new();
    private IWorld? _parent;

    public void Execute()
    {
        //This Method is mostly mirrored in ASystemGroup.QueueSystem
        //If you make changes here make sure to adjust the other method as well

        RePopulateSystemComponentAccess();

        List<Task> systemTaskCollection = new();

        var rootSystemsToProcess = new Dictionary<Identification, ASystem>(RootSystems);
        var systemJobHandles =
            rootSystemsToProcess.Keys.ToDictionary(systemId => systemId, _ => Task.CompletedTask);

        while (rootSystemsToProcess.Count > 0)
        {
            var systemsCopy = new Dictionary<Identification, ASystem>(rootSystemsToProcess);

            foreach (var (id, system) in systemsCopy)
            {
                //Check if system is active
                if (!ActiveSystems.Contains(id))
                {
                    rootSystemsToProcess.Remove(id);
                    continue;
                }

                //Check if all required systems are executed
                var missingDependency = ExecuteSystemAfter[id]
                    .Any(dependency => rootSystemsToProcess.ContainsKey(dependency));

                if (missingDependency) continue;

                //First get the tasks of the systems which writes to components where the current system needs to read from (Multiple read accesses allowed or one write access)
                var systemDependency = (from component in SystemReadComponents[id]
                    where SystemComponentAccess[component].accessType == ComponentAccessType.Write
                    select SystemComponentAccess[component].task).ToList();

                //Second, get the tasks of the systems which uses the component which the current system needs to write to
                systemDependency.AddRange(SystemWriteComponents[id]
                    .Select(component => SystemComponentAccess[component].task));

                //Third, get the tasks of the systems which needs to be executed before the current system
                systemDependency.AddRange(ExecuteSystemAfter[id].Select(dependency => systemJobHandles[dependency]));

                //Start the system execution and save its task
                system.PreExecuteMainThread();
                var systemTask = system.QueueSystem(systemDependency);
                systemTaskCollection.Add(systemTask);
                systemJobHandles[id] = systemTask;

                //Write the read component accesses of the current system to the combined task (if currently only reading tasks are present), or replace the current task if its a write access
                foreach (var component in SystemReadComponents[id])
                {
                    if (SystemComponentAccess[component].accessType == ComponentAccessType.Read)
                    {
                        var (accessType, task) = SystemComponentAccess[component];
                        SystemComponentAccess[component] = (accessType, Task.WhenAll(task, systemTask));
                        continue;
                    }

                    (ComponentAccessType, Task) componentAccess = new(ComponentAccessType.Read, systemTask);
                    SystemComponentAccess[component] = componentAccess;
                }

                //Write the write component accesses tasks of the current system
                foreach (var component in SystemWriteComponents[id])
                {
                    (ComponentAccessType, Task) componentAccess = new(ComponentAccessType.Write, systemTask);
                    SystemComponentAccess[component] = componentAccess;
                }

                //"Mark" the system as processed
                rootSystemsToProcess.Remove(id);
                _postExecuteSystems.Enqueue(system);
            }
        }

        try
        {
            //Wait for the completion of all systems
            Task.WhenAll(systemTaskCollection).Wait();
        }
        catch (AggregateException e)
        {
            foreach (var exception in e.InnerExceptions)
                Log.Error(exception, "Exception while ECS execution occured");
        }

        //Trigger the post execution for each system
        while (_postExecuteSystems.TryDequeue(out var system)) system.PostExecuteMainThread();
    }

    private void RePopulateSystemComponentAccess()
    {
        //Clears the component access, to be all an instance of a generic completed task
        foreach (var component in ComponentManager.GetComponentList())
            SystemComponentAccess.AddOrUpdate(component,
                _ => new ValueTuple<ComponentAccessType, Task>(ComponentAccessType.None, Task.CompletedTask),
                (_, _) => new ValueTuple<ComponentAccessType, Task>(ComponentAccessType.None, Task.CompletedTask));
    }

    /// <summary>
    /// Is a system marked as active
    /// </summary>
    /// <param name="systemId">Id of the system</param>
    /// <returns>False if the system is not active or doesnt exists</returns>
    public bool GetSystemActive(Identification systemId)
    {
        return ActiveSystems.Contains(systemId);
    }

    /// <summary>
    /// Set whether a system is active or not
    /// </summary>
    /// <param name="systemId">Id of the system</param>
    /// <param name="active">State the system should have</param>
    public void SetSystemActive(Identification systemId, bool active)
    {
        if (active)
        {
            ActiveSystems.Add(systemId);
            return;
        }

        ActiveSystems.Remove(systemId);
    }

    #region static setup stuff

    private static readonly Dictionary<Identification, Action<ContainerBuilder>> SystemContainerBuilderActions = new();

    public static ILifetimeScope CreateSystemLifetimeScope(ILifetimeScope parentScope) =>
        parentScope.BeginLifetimeScope("systems",
            builder =>
            {
                foreach (var (_, action) in SystemContainerBuilderActions) action(builder);
            });

    /// <summary>
    ///     Collection of components a system reads from
    /// </summary>
    internal static readonly Dictionary<Identification, HashSet<Identification>> SystemReadComponents = new();

    /// <summary>
    ///     Collection of components a system writes to
    /// </summary>
    internal static readonly Dictionary<Identification, HashSet<Identification>> SystemWriteComponents = new();

    /// <summary>
    ///     Systems which are marked as root system groups
    /// </summary>
    internal static readonly HashSet<Identification> RootSystemGroupIDs = new();

    /// <summary>
    ///     Content of all systems of a system group
    /// </summary>
    internal static readonly Dictionary<Identification, HashSet<Identification>> SystemsPerSystemGroup = new();

    /// <summary>
    ///     Collection of systems which needs to be executed before
    /// </summary>
    internal static readonly Dictionary<Identification, HashSet<Identification>> ExecuteSystemAfter = new();

    internal static readonly Dictionary<Identification, GameType> SystemExecutionSide = new();

    private static readonly Dictionary<Identification, Type> _systemTypes = new();

    /// <summary>
    /// Get the type of a system
    /// </summary>
    /// <param name="id">Id of the system</param>
    /// <returns>Type of the system</returns>
    public static Type GetSystemType(Identification id)
    {
        return _systemTypes[id];
    }

    // Helper dictionaries for Setting up the System Manager
    internal static readonly HashSet<Identification> SystemsToSort = new();
    internal static readonly Dictionary<Identification, Identification> SystemGroupPerSystem = new();

    internal static void Clear()
    {
        SystemContainerBuilderActions.Clear();
        SystemReadComponents.Clear();
        SystemWriteComponents.Clear();
        RootSystemGroupIDs.Clear();
        SystemsPerSystemGroup.Clear();
        ExecuteSystemAfter.Clear();
        SystemExecutionSide.Clear();
        SystemsToSort.Clear();
        SystemGroupPerSystem.Clear();
        _systemTypes.Clear();

        _sortSystemTypes.Clear();
        _reversedSortSystemTypes.Clear();
    }

    /// <summary>
    /// Set which components a system reads from
    /// Not intended to be used by the user. Public for source generation of component queries
    /// </summary>
    /// <param name="systemId">Id of the system</param>
    /// <param name="readComponents">Collection of read components</param>
    public static void SetReadComponents(Identification systemId, HashSet<Identification> readComponents)
    {
        while (true)
        {
            SystemReadComponents[systemId].UnionWith(readComponents);

            //Check if the current system is a root system group
            if (!RootSystemGroupIDs.Contains(systemId))
                //Recursive call with the parent SystemGroup
            {
                systemId = SystemGroupPerSystem[systemId];
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// Set which components a system writes to
    /// Not intended to be used by the user. Public for source generation of component queries
    /// </summary>
    /// <param name="systemId">Id of the system</param>
    /// <param name="writeComponents">Collection of write components</param>
    public static void SetWriteComponents(Identification systemId, HashSet<Identification> writeComponents)
    {
        while (true)
        {
            SystemWriteComponents[systemId].UnionWith(writeComponents);

            //Check if the current system is a root system group
            if (!RootSystemGroupIDs.Contains(systemId))
                //Recursive call with the parent SystemGroup
            {
                systemId = SystemGroupPerSystem[systemId];
                continue;
            }

            break;
        }
    }

    internal static void SetSystem<TSystem>(Identification systemId) where TSystem : ASystem, new()
    {
        //Remove all references to the system before
        SystemContainerBuilderActions.Remove(systemId);
        SystemsToSort.Remove(systemId);
        SystemWriteComponents.Remove(systemId);
        SystemReadComponents.Remove(systemId);
        ExecuteSystemAfter.Remove(systemId);
        _systemTypes.Remove(systemId);
        RegisterSystem<TSystem>(systemId);
    }

    internal static void RemoveSystem(Identification systemId)
    {
        SystemContainerBuilderActions.Remove(systemId);
        SystemsToSort.Remove(systemId);
        SystemWriteComponents.Remove(systemId);
        SystemReadComponents.Remove(systemId);
        ExecuteSystemAfter.Remove(systemId);
        RootSystemGroupIDs.Remove(systemId);
        SystemExecutionSide.Remove(systemId);
        SystemsToSort.Remove(systemId);
        _systemTypes.Remove(systemId);
        if (SystemGroupPerSystem.Remove(systemId, out var systemGroupId) &&
            SystemsPerSystemGroup.TryGetValue(systemGroupId, out var systemSet))
            systemSet.Remove(systemId);

        if (_sortSystemTypes.Remove(systemId, out var type))
        {
            _reversedSortSystemTypes.Remove(type);
        }
    }

    internal static void RegisterSystem<TSystem>(Identification systemId) where TSystem : ASystem
    {
        SystemContainerBuilderActions[systemId] = builder =>
        {
            builder.RegisterType<TSystem>().Keyed<ASystem>(systemId).InstancePerLifetimeScope();
        };

        SystemsToSort.Add(systemId);

        //Add the system to those dictionaries. Will be populated at System.Setup
        SystemWriteComponents.Add(systemId, new HashSet<Identification>());
        SystemReadComponents.Add(systemId, new HashSet<Identification>());
        ExecuteSystemAfter.Add(systemId, new HashSet<Identification>());
        _systemTypes.Add(systemId, typeof(TSystem));
    }

    private static void ValidateExecuteAfter(Identification systemId, Identification afterSystemId)
    {
        //Validate the correct usage of the ExecuteAfter attribute
        var isSystemRoot = RootSystemGroupIDs.Contains(systemId);
        var isToExecuteAfterRoot = RootSystemGroupIDs.Contains(afterSystemId);

        if (isSystemRoot && isToExecuteAfterRoot) return;
        
        if (isSystemRoot != isToExecuteAfterRoot)
            throw new MintyCoreException(
                "Systems to execute after have to be either in the same group or be both a root system group");
        
        if (SystemGroupPerSystem[afterSystemId] != SystemGroupPerSystem[systemId])
            throw new MintyCoreException(
                "Systems to execute after have to be either in the same group or be both a root system group");
    }

    private static void ValidateExecuteBefore(Identification systemId, Identification beforeSystemId)
    {
        //Validate the correct usage of the ExecuteBeforeAttribute
        var isSystemRoot = RootSystemGroupIDs.Contains(systemId);
        var isToExecuteBeforeRoot = RootSystemGroupIDs.Contains(beforeSystemId);

        if (isSystemRoot && isToExecuteBeforeRoot) return;
        
        if(isSystemRoot != isToExecuteBeforeRoot)
            throw new MintyCoreException(
                "Systems to execute before have to be either in the same group or be both a root system group");
        
        if(SystemGroupPerSystem[beforeSystemId] != SystemGroupPerSystem[systemId])
            throw new MintyCoreException(
                "Systems to execute before have to be either in the same group or be both a root system group");
    }

    private static readonly Dictionary<Identification, Type> _sortSystemTypes = new();
    private static readonly Dictionary<Type, Identification> _reversedSortSystemTypes = new();


    internal static void SortSystems()
    {
        var systemTypes = new Dictionary<Identification, Type>();

        //Populate helper dictionaries
        foreach (var systemId in SystemsToSort)
        {
            var systemType = GetSystemType(systemId);

            systemTypes.Add(systemId, systemType);
            _sortSystemTypes.Add(systemId, systemType);
            _reversedSortSystemTypes.Add(systemType, systemId);
        }

        DetectSystemGroups(systemTypes);

        //Sort systems into SystemGroups
        SortSystemsIntoSystemGroups();

        //Sort execution order
        SortExecutionOrder();

        //Sort execution side (client, server, both)
        SortExecutionSide();


        SystemsToSort.Clear();
    }

    private static void SortExecutionSide()
    {
        var executionSideType = typeof(ExecutionSideAttribute);

        var executionSideSort = new HashSet<Identification>(SystemsToSort);

        while (executionSideSort.Count > 0)
        {
            var copy = new HashSet<Identification>(executionSideSort);
            foreach (var systemId in copy)
            {
                var executionSide = GameType.Local;

                var isRootSystem = RootSystemGroupIDs.Contains(systemId);

                if (!(isRootSystem || SystemExecutionSide.ContainsKey(SystemGroupPerSystem[systemId]))) continue;

                if (!isRootSystem)
                {
                    if (SystemExecutionSide[SystemGroupPerSystem[systemId]] == GameType.Local)
                    {
                        if (Attribute.GetCustomAttribute(_sortSystemTypes[systemId], executionSideType) is
                            ExecutionSideAttribute
                            executionSideAtt)
                            executionSide = executionSideAtt.ExecutionSide;
                    }
                    else
                    {
                        executionSide = SystemExecutionSide[SystemGroupPerSystem[systemId]];
                    }
                }
                else
                {
                    if (Attribute.GetCustomAttribute(_sortSystemTypes[systemId], executionSideType) is
                        ExecutionSideAttribute
                        executionSideAtt)
                        executionSide = executionSideAtt.ExecutionSide;
                }

                SystemExecutionSide.Add(systemId, executionSide);
                executionSideSort.Remove(systemId);
            }
        }
    }

    private static void SortExecutionOrder()
    {
        var executeAfterType = typeof(ExecuteAfterAttribute<>);
        var executeBeforeType = typeof(ExecuteBeforeAttribute<>);
        foreach (var systemId in SystemsToSort)
        {
            var executeAfterAttributes = _sortSystemTypes[systemId].GetCustomAttributes(executeAfterType, true);
            var executeBeforeAttributes = _sortSystemTypes[systemId].GetCustomAttributes(executeBeforeType, true);

            if (!ExecuteSystemAfter.ContainsKey(systemId))
                ExecuteSystemAfter.Add(systemId, new HashSet<Identification>());

            foreach (var afterAttribute in executeAfterAttributes)
            {
                var afterSystemType = afterAttribute.GetType().GenericTypeArguments.First();
                
                if(!_reversedSortSystemTypes.TryGetValue(afterSystemType, out var afterSystemId))
                    throw new MintyCoreException($"System {afterSystemType} does not exist");
                
                ValidateExecuteAfter(systemId, afterSystemId);

                ExecuteSystemAfter[systemId].Add(afterSystemId);
            }

            foreach (var beforeAttribute in executeBeforeAttributes)
            {
                var beforeSystemType = beforeAttribute.GetType().GenericTypeArguments.First();
                
                if(!_reversedSortSystemTypes.TryGetValue(beforeSystemType, out var beforeSystemId))
                    throw new MintyCoreException($"System {beforeSystemType} does not exist");

                ValidateExecuteBefore(systemId, beforeSystemId);

                if (!ExecuteSystemAfter.ContainsKey(beforeSystemId))
                    ExecuteSystemAfter.Add(beforeSystemId, new HashSet<Identification>());

                ExecuteSystemAfter[beforeSystemId].Add(systemId);
            }
        }
    }

    private static void SortSystemsIntoSystemGroups()
    {
        var executeInSystemGroupType = typeof(ExecuteInSystemGroupAttribute<>);

        foreach (var systemId in SystemsToSort.Where(systemId => !RootSystemGroupIDs.Contains(systemId)))
        {
            var systemType = _sortSystemTypes[systemId];
            var systemGroupAttribute = systemType.GetCustomAttributes(executeInSystemGroupType, true);

            if (systemGroupAttribute.Length == 0)
            {
                SystemsPerSystemGroup[SystemIDs.SimulationGroup].Add(systemId);
                SystemGroupPerSystem.Add(systemId, SystemIDs.SimulationGroup);
                continue;
            }

            var systemGroupType = systemGroupAttribute.First().GetType().GenericTypeArguments.First();
            
            if(!_reversedSortSystemTypes.TryGetValue(systemGroupType, out var systemGroupId))
                throw new MintyCoreException($"SystemGroup {systemGroupType} does not exist");
            
            SystemsPerSystemGroup[systemGroupId].Add(systemId);
            SystemGroupPerSystem.Add(systemId, systemGroupId);
        }
    }

    private static void DetectSystemGroups(Dictionary<Identification, Type> systemTypes)
    {
        //Detect SystemGroups
        var rootSystemGroupType = typeof(RootSystemGroupAttribute);
        foreach (var systemId in SystemsToSort.Where(systemId =>
                     systemTypes[systemId].IsSubclassOf(typeof(ASystemGroup))))
        {
            if (Attribute.IsDefined(_sortSystemTypes[systemId], rootSystemGroupType)) RootSystemGroupIDs.Add(systemId);

            SystemsPerSystemGroup.Add(systemId, new HashSet<Identification>());
        }
    }

    #endregion
}

internal enum ComponentAccessType
{
    None = 0,
    Read,
    Write
}