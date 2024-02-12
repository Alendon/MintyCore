using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Network.Messages;

/// <summary>
/// Information message to tell a client which mods to load and which ids to assign
/// </summary>
[RegisterMessage("load_mods")]
public partial class LoadMods : IMessage
{
    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.LoadMods;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;
    
    /// <summary/>
    public required IModManager ModManager { private get; [UsedImplicitly] init; }
    private IRegistryManager RegistryManager => ModManager.RegistryManager;
    
    /// <summary/>
    public required INetworkHandler NetworkHandler { get; init; }



    /// <inheritdoc />
    public ushort Sender { get; set; }

    /// <summary>
    /// Collection of mods to load including their versions
    /// </summary>
    public IEnumerable<(string modId, Version modVersion)> Mods =
        Enumerable.Empty<(string modId, Version modVersion)>();

    /// <summary>
    /// Collection of mod ids
    /// </summary>
    public IEnumerable<KeyValuePair<ushort, string>> ModIDs = Enumerable.Empty<KeyValuePair<ushort, string>>();

    /// <summary>
    /// Collection of category ids
    /// </summary>
    public IEnumerable<KeyValuePair<ushort, string>> CategoryIDs = Enumerable.Empty<KeyValuePair<ushort, string>>();

    /// <summary>
    /// Collection of object ids
    /// </summary>
    public IEnumerable<KeyValuePair<Identification, string>> ObjectIDs =
        Enumerable.Empty<KeyValuePair<Identification, string>>();


    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        var modCount = Mods.Count();
        var modIDsCount = ModIDs.Count();
        var categoryIDsCount = CategoryIDs.Count();
        var objectIDsCount = CategoryIDs.Count();

        writer.Put(modCount);
        writer.Put(modIDsCount);
        writer.Put(categoryIDsCount);
        writer.Put(objectIDsCount);

        foreach (var (modId, modVersion) in Mods)
        {
            writer.Put(modId);
            writer.Put(modVersion);
        }

        foreach (var (modId, modStringId) in ModIDs)
        {
            writer.Put(modId);
            writer.Put(modStringId);
        }

        foreach (var (categoryId, categoryStringId) in CategoryIDs)
        {
            writer.Put(categoryId);
            writer.Put(categoryStringId);
        }

        foreach (var (objectId, objectStringId) in ObjectIDs)
        {
            objectId.Serialize(writer);
            writer.Put(objectStringId);
        }
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    {
        if (IsServer) return false;

        if (!reader.TryGetInt(out var modCount) ||
            !reader.TryGetInt(out var modIDsCount) ||
            !reader.TryGetInt(out var categoryIDsCount) ||
            !reader.TryGetInt(out var objectIDsCount))
        {
            Log.Error("Failed to deserialize {Message} header", nameof(LoadMods));
            return false;
        }


        var mods = new (string modId, Version modVersion)[modCount];
        var modIds = new Dictionary<ushort, string>(modIDsCount);
        var categoryIds = new Dictionary<ushort, string>(categoryIDsCount);
        var objectIds = new Dictionary<Identification, string>(objectIDsCount);

        for (var i = 0; i < modCount; i++)
        {
            if (reader.TryGetString(out mods[i].modId) &&
                reader.TryGetVersion(out var version))
            {
                mods[i].modVersion = version;
                continue;
            }

            Log.Information("Failed to deserialize mods to load");
            return false;
        }

        for (var i = 0; i < modIDsCount; i++)
        {
            if (reader.TryGetUShort(out var numericId) && reader.TryGetString(out var stringId))
            {
                modIds.Add(numericId, stringId);
                continue;
            }
            Log.Error("Failed to deserialize mod ID's");
            return false;
        }

        for (var i = 0; i < categoryIDsCount; i++)
        {
            if (reader.TryGetUShort(out var numericId) && reader.TryGetString(out var stringId))
            {
                categoryIds.Add(numericId, stringId);
                continue;
            }
            Log.Error("Failed to deserialize category ID's");
            return false;
        }

        for (var i = 0; i < objectIDsCount; i++)
        {
            if (Identification.Deserialize(reader, out var numericId) && reader.TryGetString(out var stringId))
            {
                objectIds.Add(numericId, stringId);
                continue;
            }
            Log.Error("Failed to deserialize object ID's");
            return false;
        }

        Mods = mods;
        ModIDs = new ReadOnlyDictionary<ushort, string>(modIds);
        CategoryIDs = new ReadOnlyDictionary<ushort, string>(categoryIds);
        ObjectIDs = new ReadOnlyDictionary<Identification, string>(objectIds);

        if (Engine.GameType != GameType.Client) return true;

        //TODO Change this to a custom Engine method

        RegistryManager.SetModIDs(ModIDs);
        RegistryManager.SetCategoryIDs(CategoryIDs);
        RegistryManager.SetObjectIDs(ObjectIDs);

        var modInfosToLoad =
            from modInfos in ModManager.GetAvailableMods(false)
            from modsToLoad in Mods
            where modInfos.Identifier.Equals(modsToLoad.modId) &&
                  modInfos.Version.CompatibleWith(modsToLoad.modVersion)
            select modInfos;

        ModManager.LoadGameMods(modInfosToLoad);

        return true;
    }

    /// <inheritdoc />
    public void Clear()
    {
        Mods = Enumerable.Empty<(string modId, Version modVersion)>();
        ModIDs = Enumerable.Empty<KeyValuePair<ushort, string>>();
        CategoryIDs = Enumerable.Empty<KeyValuePair<ushort, string>>();
        ObjectIDs = Enumerable.Empty<KeyValuePair<Identification, string>>();
    }
}