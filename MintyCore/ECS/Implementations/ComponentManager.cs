using System;
using System.Collections.Generic;
using Autofac;
using MintyCore.Modding;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.ECS.Implementations;

/// <summary>
///     Class to manage component stuff at init and runtime
/// </summary>
[Singleton<IComponentManager>]
internal class ComponentManager(IModManager modManager) : IComponentManager
{
    //Most of the following data is stored, as at runtime only the pointers of the component data and the id of the components are present
    //And in C# there is no possibility to "store" the type of the component

    /// <summary>
    ///     The size of each component in bytes
    /// </summary>
    private readonly Dictionary<Identification, int> _componentSizes = new();

    /// <summary>
    ///     Methods to set the default values of each component
    /// </summary>
    private readonly Dictionary<Identification, Action<IntPtr>> _componentDefaultValues = new();

    /// <summary>
    ///     The Serialization methods of each component
    /// </summary>
    private readonly Dictionary<Identification, Action<IntPtr, DataWriter, IWorld, Entity>>
        _componentSerializeActions = new();

    /// <summary>
    ///     The Deserialization methods of each component
    /// </summary>
    private readonly Dictionary<Identification, Func<IntPtr, DataReader, IWorld, Entity, bool>>
        _componentDeserialize = new();

    private readonly Dictionary<Identification, Action<ContainerBuilder>> _componentSerializerBuilders = new();
    private readonly Dictionary<Identification, ComponentSerializer> _componentSerializers = new();
    private ILifetimeScope? _componentSerializerScope;

    /// <summary>
    ///     Methods to cast the pointer to the IComponent interface of each component (value of the pointer will be boxed)
    /// </summary>
    private readonly Dictionary<Identification, Func<IntPtr, IComponent>> _ptrToComponentCasts = new();

    private readonly Dictionary<Identification, Type?> _componentTypes = new();

    /// <summary>
    ///     Which components are controlled by players (players send the updates to the server for them)
    /// </summary>
    private readonly HashSet<Identification> _playerControlledComponents = new();

    public unsafe void AddComponent<TComponent>(Identification componentId)
        where TComponent : unmanaged, IComponent
    {
        if (_componentSizes.ContainsKey(componentId))
        {
            throw new MintyCoreException($"Component {componentId} is already registered");
        }

        _componentSizes.Add(componentId, sizeof(TComponent));
        _componentDefaultValues.Add(componentId, ptr =>
        {
            *(TComponent*)ptr = default;
            ((TComponent*)ptr)->PopulateWithDefaultValues();
        });

        _componentSerializeActions.Add(componentId,
            (ptr, serializer, world, entity) => { ((TComponent*)ptr)->Serialize(serializer, world, entity); });

        _componentDeserialize.Add(componentId,
            (ptr, deserializer, world, entity) => ((TComponent*)ptr)->Deserialize(deserializer, world, entity));

        _ptrToComponentCasts.Add(componentId, ptr => *(TComponent*)ptr);

        var componentType = typeof(TComponent);
        //Check if the component has the [PlayerControlledAtrribute]
        if (Array.Exists(componentType.GetCustomAttributes(false),
                attribute => attribute.GetType() == typeof(PlayerControlledAttribute)))
            _playerControlledComponents.Add(componentId);

        _componentTypes.Add(componentId, typeof(TComponent));
    }

    public void AddComponentSerializer<TComponentSerializer>(Identification serializerId) where TComponentSerializer : ComponentSerializer
    {
        _componentSerializerBuilders.Add(serializerId, builder => builder.RegisterType<TComponentSerializer>().Keyed<ComponentSerializer>(serializerId));
    }

    public void RemoveComponentSerializer(Identification objectId)
    {
        _componentSerializerBuilders.Remove(objectId);
    }

    public void BuildComponentSerializers()
    {
        if (_componentSerializerScope is not null)
            throw new MintyCoreException("Component serializers are already built");

        _componentSerializerScope = modManager.ModLifetimeScope.BeginLifetimeScope(b =>
        {
            foreach (var (_, builder) in _componentSerializerBuilders)
                builder(b);
        });

        foreach (var (id, _) in _componentSerializerBuilders)
        {
            var serializer = _componentSerializerScope.ResolveKeyed<ComponentSerializer>(id);
            _componentSerializers.Add(serializer.ComponentId, serializer);
        }
    }

    public void DestroyComponentSerializers()
    {
        _componentSerializerScope?.Dispose();
        _componentSerializerScope = null;
        _componentSerializers.Clear();
    }

    /// <summary>
    ///     Check if a <see cref="IComponent" /> is player controlled
    /// </summary>
    public bool IsPlayerControlled(Identification componentId)
    {
        return _playerControlledComponents.Contains(componentId);
    }

    /// <summary>
    ///     Get the size in bytes of a <see cref="IComponent" />
    /// </summary>
    /// <param name="componentId"><see cref="Identification" /> of the component</param>
    /// <returns>Offset in bytes</returns>
    public int GetComponentSize(Identification componentId)
    {
        return _componentSizes[componentId];
    }

    /// <summary>
    ///     Serialize a component
    /// </summary>
    public void SerializeComponent(IntPtr component, Identification componentId, DataWriter dataWriter,
        IWorld world, Entity entity)
    {
        if (_componentSerializers.TryGetValue(componentId, out var serializer))
            serializer.Serialize(component, dataWriter, world, entity);
        else
            _componentSerializeActions[componentId](component, dataWriter, world, entity);
    }

    /// <summary>
    ///     Deserialize a component
    /// </summary>
    /// <returns>True if deserialization was successful</returns>
    public bool DeserializeComponent(IntPtr component, Identification componentId, DataReader dataReader,
        IWorld world, Entity entity)
    {
        if(_componentSerializers.TryGetValue(componentId, out var serializer))
            return serializer.Deserialize(component, dataReader, world, entity);
        
        return _componentDeserialize[componentId](component, dataReader, world, entity);
    }

    public void PopulateComponentDefaultValues(Identification componentId, IntPtr componentLocation)
    {
        _componentDefaultValues[componentId](componentLocation);
    }

    /// <summary>
    ///     Cast a <see cref="IntPtr" /> to <see cref="IComponent" /> by the given component <see cref="Identification" />
    /// </summary>
    /// <param name="componentId"><see cref="Identification" /> of the component</param>
    /// <param name="componentPtr">Location of the component in memory</param>
    /// <returns><see cref="IComponent" /> parent of the component</returns>
    public IComponent CastPtrToIComponent(Identification componentId, IntPtr componentPtr)
    {
        return _ptrToComponentCasts[componentId](componentPtr);
    }


    public void Clear()
    {
        _componentSizes.Clear();
        _componentDefaultValues.Clear();
        _playerControlledComponents.Clear();
        _componentSerializeActions.Clear();
        _componentDeserialize.Clear();
        _ptrToComponentCasts.Clear();
    }

    public IEnumerable<Identification> GetComponentList()
    {
        return _componentSizes.Keys;
    }

    public void RemoveComponent(Identification objectId)
    {
        if (!_componentSizes.Remove(objectId))
            Log.Warning("Component to remove {ObjectId} is not present", objectId);

        _componentDefaultValues.Remove(objectId);
        _playerControlledComponents.Remove(objectId);
        _componentSerializeActions.Remove(objectId);
        _componentDeserialize.Remove(objectId);
        _ptrToComponentCasts.Remove(objectId);
    }

    /// <summary>
    /// Get the type of a component
    /// </summary>
    /// <param name="componentId"></param>
    /// <returns></returns>
    public Type? GetComponentType(Identification componentId)
    {
        return _componentTypes[componentId];
    }
}