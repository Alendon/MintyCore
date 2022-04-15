using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     Class to manage component stuff at init and runtime
/// </summary>
public static class ComponentManager
{
    //Most of the following data is stored, as at runtime only the pointers of the component data and the id of the components are present
    //And in C# there is no possibility to "store" the type of the component

    /// <summary>
    ///     The size of each component in bytes
    /// </summary>
    private static readonly Dictionary<Identification, int> _componentSizes = new();

    /// <summary>
    ///     Methods to set the default values of each component
    /// </summary>
    private static readonly Dictionary<Identification, Action<IntPtr>> _componentDefaultValues = new();

    /// <summary>
    ///     The Serialization methods of each component
    /// </summary>
    private static readonly Dictionary<Identification, Action<IntPtr, DataWriter, IWorld, Entity>>
        _componentSerialize = new();

    /// <summary>
    ///     The Deserialization methods of each component
    /// </summary>
    private static readonly Dictionary<Identification, Func<IntPtr, DataReader, IWorld, Entity, bool>>
        _componentDeserialize = new();

    /// <summary>
    ///     Methods to cast the pointer to the IComponent interface of each component (value of the pointer will be boxed)
    /// </summary>
    private static readonly Dictionary<Identification, Func<IntPtr, IComponent>> _ptrToComponentCasts = new();

    private static readonly Dictionary<Identification, Type?> _componentTypes = new();

    /// <summary>
    ///     Which components are controlled by players (players send the updates to the server for them)
    /// </summary>
    private static readonly HashSet<Identification> _playerControlledComponents = new();

    internal static void SetComponent<TComponent>(Identification id) where TComponent : unmanaged, IComponent
    {
        _componentSizes.Remove(id);
        _componentDefaultValues.Remove(id);
        _componentSerialize.Remove(id);
        _componentDeserialize.Remove(id);
        _ptrToComponentCasts.Remove(id);
        _playerControlledComponents.Remove(id);
        _componentTypes.Remove(id);
        AddComponent<TComponent>(id);
    }

    internal static unsafe void AddComponent<TComponent>(Identification componentId) where TComponent : unmanaged, IComponent
    {
        Logger.AssertAndThrow(!_componentSizes.ContainsKey(componentId),
            $"Component {componentId} is already registered", "ECS");

        _componentSizes.Add(componentId, sizeof(TComponent));
        _componentDefaultValues.Add(componentId, ptr =>
        {
            *(TComponent*) ptr = default;
            ((TComponent*) ptr)->PopulateWithDefaultValues();
        });

        _componentSerialize.Add(componentId,
            (ptr, serializer, world, entity) => { ((TComponent*) ptr)->Serialize(serializer, world, entity); });

        _componentDeserialize.Add(componentId,
            (ptr, deserializer, world, entity) => ((TComponent*) ptr)->Deserialize(deserializer, world, entity));

        _ptrToComponentCasts.Add(componentId, ptr => *(TComponent*) ptr);

        var componentType = typeof(TComponent);
        //Check if the component has the [PlayerControlledAtrribute]
        if (componentType.GetCustomAttributes(false)
            .Any(attribute => attribute.GetType() == typeof(PlayerControlledAttribute)))
            _playerControlledComponents.Add(componentId);
        
        _componentTypes.Add(componentId, typeof(TComponent));
    }

    /// <summary>
    ///     Check if a <see cref="IComponent" /> is player controlled
    /// </summary>
    public static bool IsPlayerControlled(Identification componentId)
    {
        return _playerControlledComponents.Contains(componentId);
    }

    /// <summary>
    ///     Get the size in bytes of a <see cref="IComponent" />
    /// </summary>
    /// <param name="componentId"><see cref="Identification" /> of the component</param>
    /// <returns>Offset in bytes</returns>
    public static int GetComponentSize(Identification componentId)
    {
        return _componentSizes[componentId];
    }

    /// <summary>
    ///     Serialize a component
    /// </summary>
    public static void SerializeComponent(IntPtr component, Identification componentId, DataWriter dataWriter,
        IWorld world, Entity entity)
    {
        _componentSerialize[componentId](component, dataWriter, world, entity);
    }

    /// <summary>
    ///     Deserialize a component
    /// </summary>
    /// <returns>True if deserialization was successful</returns>
    public static bool DeserializeComponent(IntPtr component, Identification componentId, DataReader dataReader,
        IWorld world, Entity entity)
    {
        return _componentDeserialize[componentId](component, dataReader, world, entity);
    }

    internal static void PopulateComponentDefaultValues(Identification componentId, IntPtr componentLocation)
    {
        _componentDefaultValues[componentId](componentLocation);
    }

    /// <summary>
    ///     Cast a <see cref="IntPtr" /> to <see cref="IComponent" /> by the given component <see cref="Identification" />
    /// </summary>
    /// <param name="componentId"><see cref="Identification" /> of the component</param>
    /// <param name="componentPtr">Location of the component in memory</param>
    /// <returns><see cref="IComponent" /> parent of the component</returns>
    public static IComponent CastPtrToIComponent(Identification componentId, IntPtr componentPtr)
    {
        return _ptrToComponentCasts[componentId](componentPtr);
    }


    internal static void Clear()
    {
        _componentSizes.Clear();
        _componentDefaultValues.Clear();
        _playerControlledComponents.Clear();
        _componentSerialize.Clear();
        _componentDeserialize.Clear();
        _ptrToComponentCasts.Clear();
    }

    internal static IEnumerable<Identification> GetComponentList()
    {
        return _componentSizes.Keys;
    }

    internal static void RemoveComponent(Identification objectId)
    {
        Logger.AssertAndLog(_componentSizes.Remove(objectId), $"Component to remove {objectId} is not present", "ECS",
            LogImportance.WARNING);

        _componentDefaultValues.Remove(objectId);
        _playerControlledComponents.Remove(objectId);
        _componentSerialize.Remove(objectId);
        _componentDeserialize.Remove(objectId);
        _ptrToComponentCasts.Remove(objectId);
    }

    public static Type? GetComponentType(Identification componentId)
    {
        return _componentTypes[componentId];
    }
}