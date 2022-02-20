﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.ECS;

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
        if (executeAfter.Any(type => Activator.CreateInstance(type) is not ASystem))
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
        if (executeBefore.Any(type => Activator.CreateInstance(type) is not ASystem))
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
///     <see cref="Registries.SystemRegistry" /> and manages the systems for a <see cref="World" />
/// </summary>
public class SystemManager : IDisposable
{
    internal readonly HashSet<Identification> InactiveSystems = new();

    internal readonly World Parent;

    /// <summary>
    ///     Stores how a component (key) is accessed and the task by the system(s) which is using it
    /// </summary>
    internal readonly ConcurrentDictionary<Identification, (ComponentAccessType accessType, Task task)>
        SystemComponentAccess =
            new();

    /// <summary>
    ///     Root systems of this manager instance. Those are commonly system groups which contains other systems
    /// </summary>
    internal Dictionary<Identification, ASystem> RootSystems = new();

    /// <summary>
    ///     Create a new SystemManager for <paramref name="world" />
    /// </summary>
    /// <param name="world"></param>
    public SystemManager(World world)
    {
        Parent = world;

        //Iterate and filter all registered root systems
        //and add the remaining ones as to the system group and initialize them
        foreach (var systemId in RootSystemGroupIDs.Where(systemId =>
                     (SystemExecutionSide[systemId].HasFlag(GameType.SERVER) || !world.IsServerWorld) &&
                     (SystemExecutionSide[systemId].HasFlag(GameType.CLIENT) || world.IsServerWorld)))
        {
            RootSystems.Add(systemId, SystemCreateFunctions[systemId](Parent));
            RootSystems[systemId].Setup();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var (_, system) in RootSystems) system.Dispose();
    }

    internal void Execute()
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
                if (InactiveSystems.Contains(id))
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
                    where SystemComponentAccess[component].accessType == ComponentAccessType.WRITE
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
                    if (SystemComponentAccess[component].accessType == ComponentAccessType.READ)
                    {
                        var (accessType, task) = SystemComponentAccess[component];
                        SystemComponentAccess[component] = (accessType, Task.WhenAll(task, systemTask));
                        continue;
                    }

                    (ComponentAccessType, Task) componentAccess = new(ComponentAccessType.READ, systemTask);
                    SystemComponentAccess[component] = componentAccess;
                }

                //Write the write component accesses tasks of the current system
                foreach (var component in SystemWriteComponents[id])
                {
                    (ComponentAccessType, Task) componentAccess = new(ComponentAccessType.WRITE, systemTask);
                    SystemComponentAccess[component] = componentAccess;
                }

                //"Mark" the system as processed
                rootSystemsToProcess.Remove(id);
            }
        }


        //Wait for the completion of all systems
        Task.WhenAll(systemTaskCollection).Wait();

        //Trigger the post execution for each system
        foreach (var system in RootSystems) system.Value.PostExecuteMainThread();
    }

    private void RePopulateSystemComponentAccess()
    {
        //Clears the component access, to be all an instance of a generic completed task
        foreach (var component in ComponentManager.GetComponentList())
            SystemComponentAccess.AddOrUpdate(component,
                _ => new ValueTuple<ComponentAccessType, Task>(ComponentAccessType.NONE, Task.CompletedTask),
                (_, _) => new ValueTuple<ComponentAccessType, Task>(ComponentAccessType.NONE, Task.CompletedTask));
    }

    #region static setup stuff

    /// <summary>
    ///     Create functions for each system
    /// </summary>
    internal static readonly Dictionary<Identification, Func<World?, ASystem>> SystemCreateFunctions = new();

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

    // Helper dictionaries for Setting up the System Manager
    internal static readonly HashSet<Identification> SystemsToSort = new();
    internal static readonly Dictionary<Identification, Identification> SystemGroupPerSystem = new();

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

    internal static void SetSystem<TSystem>(Identification systemId) where TSystem : ASystem, new()
    {
        //Remove all references to the system before
        SystemCreateFunctions.Remove(systemId);
        SystemsToSort.Remove(systemId);
        SystemWriteComponents.Remove(systemId);
        SystemReadComponents.Remove(systemId);
        ExecuteSystemAfter.Remove(systemId);
        RegisterSystem<TSystem>(systemId);
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

        //Add the system to those dictionaries. Will be populated at System.Setup
        SystemWriteComponents.Add(systemId, new HashSet<Identification>());
        SystemReadComponents.Add(systemId, new HashSet<Identification>());
        ExecuteSystemAfter.Add(systemId, new HashSet<Identification>());
    }

    private static void ValidateExecuteAfter(Identification systemId, Identification afterSystemId)
    {
        //Validate the correct usage of the ExecuteAfter attribute
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
        //Validate the correct usage of the ExecuteBeforeAttribute
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
        foreach (var systemId in SystemsToSort.Where(systemId => systemInstances[systemId] is ASystemGroup))
        {
            if (Attribute.IsDefined(systemTypes[systemId], rootSystemGroupType)) RootSystemGroupIDs.Add(systemId);

            SystemsPerSystemGroup.Add(systemId, new HashSet<Identification>());
        }

        //Sort systems into SystemGroups
        var executeInSystemGroupType = typeof(ExecuteInSystemGroupAttribute);

        foreach (var systemId in SystemsToSort.Where(systemId => !RootSystemGroupIDs.Contains(systemId)))
        {
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

        var executionSideSort = new HashSet<Identification>(SystemsToSort);

        while (executionSideSort.Count > 0)
        {
            var copy = new HashSet<Identification>(executionSideSort);
            foreach (var systemId in copy)
            {
                var executionSide = GameType.LOCAL;

                var isRootSystem = RootSystemGroupIDs.Contains(systemId);

                if (!(isRootSystem || SystemExecutionSide.ContainsKey(SystemGroupPerSystem[systemId]))) continue;

                if (!isRootSystem)
                {
                    if (SystemExecutionSide[SystemGroupPerSystem[systemId]] == GameType.LOCAL)
                    {
                        if (Attribute.GetCustomAttribute(systemTypes[systemId], executionSideType) is
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
                    if (Attribute.GetCustomAttribute(systemTypes[systemId], executionSideType) is
                        ExecutionSideAttribute
                        executionSideAtt)
                        executionSide = executionSideAtt.ExecutionSide;
                }

                SystemExecutionSide.Add(systemId, executionSide);
                executionSideSort.Remove(systemId);
            }
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