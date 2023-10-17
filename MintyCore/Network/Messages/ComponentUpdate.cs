using System;
using System.Collections.Generic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

//TODO we probably need to completely rewrite this as this just sends the updates for all entities to all players
/// <summary>
///     Message to update components of entities
/// </summary>
[RegisterMessage("component_update")]
public partial class ComponentUpdate : IMessage
{
    public required IWorldHandler WorldHandler { private get; init; }
    public required IComponentManager ComponentManager { private get; init; }
    public required INetworkHandler NetworkHandler { get; init; }


    
    private Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>>? _components;

    /// <summary>
    ///     Collection of components to update
    /// </summary>
    public Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>> Components =>
        _components ??= GetComponentsListDictionary();

    /// <summary>
    ///     The world id the components live in
    /// </summary>
    public Identification WorldId { get; set; }

    /// <summary>
    ///     The world game type (client or server)
    /// </summary>
    public GameType WorldGameType { get; set; }

    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.ComponentUpdate;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;

    /// <inheritdoc />
    public ushort Sender { get; set; }

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        WorldId.Serialize(writer);

        writer.Put(Components.Count);

        if (Components.Count == 0 || !WorldHandler.TryGetWorld(WorldGameType, WorldId, out var world)) return;

        foreach (var (entity, components) in Components)
        {
            writer.EnterRegion(Engine.TestingModeActive ? entity.ToString() : null);
            entity.Serialize(writer);

            writer.Put(components.Count);
            foreach (var (componentId, componentData) in components)
            {
                writer.EnterRegion(Engine.TestingModeActive ? componentId.ToString() : null);
                componentId.Serialize(writer);
                ComponentManager.SerializeComponent(componentData, componentId, writer, world, entity);
                writer.ExitRegion();
            }

            writer.ExitRegion();
        }
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    {
        if (!Identification.Deserialize(reader, out var worldId))
        {
            Logger.WriteLog("Failed to deserialize world id", LogImportance.Error, "Network");
            return false;
        }

        var worldType = IsServer ? GameType.Server : GameType.Client;
        if (!WorldHandler.TryGetWorld(worldType, worldId, out var world))
        {
            Logger.WriteLog($"Failed to fetch {(IsServer ? "server" : "client")} world {worldId}", LogImportance.Error,
                "Network");
            return false;
        }

        if (!reader.TryGetInt(out var entityCount))
        {
            Logger.WriteLog("Failed to deserialize entity count", LogImportance.Error, "Network");
            return false;
        }


        for (var i = 0; i < entityCount; i++)
        {
            DeserializeEntity(reader, world);
        }

        return true;
    }

    private void DeserializeEntity(DataReader reader, IWorld world)
    {
        reader.EnterRegion();
        if (!Entity.Deserialize(reader, out var entity))
        {
            Logger.WriteLog("Failed to deserialize entity identification", LogImportance.Error, "Network");

            reader.ExitRegion();
            return;
        }

        if (!world.EntityManager.EntityExists(entity))
        {
            Logger.WriteLog($"Entity {entity} to deserialize does not exists locally", LogImportance.Info,
                "Network");

            reader.ExitRegion();
            return;
        }

        if (!reader.TryGetInt(out var componentCount))
        {
            Logger.WriteLog($"Failed to deserialize component count for Entity {entity}", LogImportance.Error,
                "Network");

            reader.ExitRegion();
            return;
        }

        for (var j = 0; j < componentCount; j++)
        {
            DeserializeComponent(reader, world, entity);
        }

        reader.ExitRegion();
    }

    private void DeserializeComponent(DataReader reader, IWorld world, Entity entity)
    {
        reader.EnterRegion();
        if (!Identification.Deserialize(reader, out var componentId))
        {
            Logger.WriteLog("Failed to deserialize component id", LogImportance.Error, "Network");
            reader.ExitRegion();
            return;
        }

        switch (IsServer)
        {
            case true when !ComponentManager.IsPlayerControlled(componentId):
            case false when ComponentManager.IsPlayerControlled(componentId):
            case true when ComponentManager.IsPlayerControlled(componentId) &&
                           world.EntityManager.GetEntityOwner(entity) != Sender:
                reader.ExitRegion();
                return;
        }

        var componentPtr = world.EntityManager.GetComponentPtr(entity, componentId);
        if (!ComponentManager.DeserializeComponent(componentPtr,
                componentId, reader, world, entity))
            Logger.WriteLog($"Failed to deserialize component {componentId} from {entity}", LogImportance.Error,
                "Network");

        reader.ExitRegion();
    }

    /// <inheritdoc />
    public void Clear()
    {
        ReturnComponentsListDictionary(Components);
        _components = null;
    }

    private static readonly Queue<List<(Identification componentId, IntPtr componentData)>>
        ComponentsListPool = new();

    private static readonly Queue<Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>>>
        ComponentsListDictionary = new();

    internal static List<(Identification componentId, IntPtr componentData)> GetComponentsList()
    {
        return ComponentsListPool.Count > 0
            ? ComponentsListPool.Dequeue()
            : new List<(Identification componentId, IntPtr componentData)>();
    }

    private static void ReturnComponentsList(List<(Identification componentId, IntPtr componentData)> list)
    {
        list.Clear();
        ComponentsListPool.Enqueue(list);
    }

    private static Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>>
        GetComponentsListDictionary()
    {
        return ComponentsListDictionary.Count > 0
            ? ComponentsListDictionary.Dequeue()
            : new Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>>();
    }

    private static void ReturnComponentsListDictionary(
        Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>> dictionary)
    {
        foreach (var list in dictionary.Values) ReturnComponentsList(list);

        dictionary.Clear();
        ComponentsListDictionary.Enqueue(dictionary);
    }
}