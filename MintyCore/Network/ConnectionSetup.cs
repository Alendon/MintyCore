using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MintyCore.Modding;
using MintyCore.Utils;

namespace MintyCore.Network;

internal struct PlayerInformation
{
    public string PlayerName;
    public ulong PlayerId;
    public IEnumerable<(string modId, ModVersion version)> AvailableMods;

    public void Serialize(DataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(PlayerName);

        writer.Put(AvailableMods.Count());
        foreach (var (modId, modVersion) in AvailableMods)
        {
            writer.Put(modId);
            modVersion.Serialize(writer);
        }
    }

    public bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetULong(out var playerId) || !reader.TryGetString(out var playerName) ||
            !reader.TryGetInt(out var modCount))
        {
            Logger.WriteLog("Failed to deserialize connection setup data", LogImportance.Error, "Network");
            return false;
        }

        PlayerId = playerId;
        PlayerName = playerName;

        var mods = new (string modId, ModVersion version)[modCount];

        for (var i = 0; i < modCount; i++)
        {
            if (reader.TryGetString(out mods[i].modId) && ModVersion.Deserialize(reader, out mods[i].version)) continue;

            Logger.WriteLog("Failed to deserialize mod information's", LogImportance.Error, "Network");
            return false;
        }

        AvailableMods = mods;
        return true;
    }
}

internal struct PlayerConnected
{
    public ushort PlayerGameId;

    public void Serialize(DataWriter writer)
    {
        writer.Put(PlayerGameId);
    }

    public bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetUShort(out var gameId)) return false;

        PlayerGameId = gameId;
        return true;
    }
}

internal struct LoadMods
{
    public IEnumerable<(string modId, ModVersion modVersion)> Mods { get; set; }
    public ReadOnlyDictionary<ushort, string> ModIDs { get; set; }
    public ReadOnlyDictionary<ushort, string> CategoryIDs { get; set; }
    public ReadOnlyDictionary<Identification, string> ObjectIDs { get; set; }

    public void Serialize(DataWriter writer)
    {
        var modCount = Mods.Count();
        var modIDsCount = ModIDs.Count;
        var categoryIDsCount = CategoryIDs.Count;
        var objectIDsCount = CategoryIDs.Count;

        writer.Put(modCount);
        writer.Put(modIDsCount);
        writer.Put(categoryIDsCount);
        writer.Put(objectIDsCount);

        foreach (var (modId, modVersion) in Mods)
        {
            writer.Put(modId);
            modVersion.Serialize(writer);
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

    public bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetInt(out var modCount) ||
            !reader.TryGetInt(out var modIDsCount) ||
            !reader.TryGetInt(out var categoryIDsCount) ||
            !reader.TryGetInt(out var objectIDsCount))
        {
            Logger.WriteLog($"Failed to deserialize {nameof(LoadMods)} header", LogImportance.Error, "Network");
            return false;
        }


        var mods = new (string modId, ModVersion modVersion)[modCount];
        var modIds = new Dictionary<ushort, string>(modIDsCount);
        var categoryIds = new Dictionary<ushort, string>(categoryIDsCount);
        var objectIds = new Dictionary<Identification, string>(objectIDsCount);

        for (var i = 0; i < modCount; i++)
        {
            if (reader.TryGetString(out mods[i].modId) &&
                ModVersion.Deserialize(reader, out mods[i].modVersion)) continue;

            Logger.WriteLog("Failed to deserialize mods to load", LogImportance.Error, "Network");
            return false;
        }

        for (var i = 0; i < modIDsCount; i++)
        {
            if (reader.TryGetUShort(out var numericId) && reader.TryGetString(out var stringId))
            {
                modIds.Add(numericId, stringId);
                continue;
            }

            Logger.WriteLog("Failed to deserialize mod ids", LogImportance.Error, "Network");

            return false;
        }

        for (var i = 0; i < categoryIDsCount; i++)
        {
            if (reader.TryGetUShort(out var numericId) && reader.TryGetString(out var stringId))
            {
                categoryIds.Add(numericId, stringId);
                continue;
            }

            Logger.WriteLog("Failed to deserialize category ids", LogImportance.Error, "Network");

            return false;
        }

        for (var i = 0; i < objectIDsCount; i++)
        {
            if (Identification.Deserialize(reader, out var numericId) && reader.TryGetString(out var stringId))
            {
                objectIds.Add(numericId, stringId);
                continue;
            }

            Logger.WriteLog("Failed to deserialize object ids", LogImportance.Error, "Network");

            return false;
        }

        Mods = mods;
        ModIDs = new ReadOnlyDictionary<ushort, string>(modIds);
        CategoryIDs = new ReadOnlyDictionary<ushort, string>(categoryIds);
        ObjectIDs = new ReadOnlyDictionary<Identification, string>(objectIds);

        return true;
    }
}

internal enum ConnectionSetupMessageType
{
    Invalid = Constants.InvalidId,
    PlayerInformation,
    PlayerConnected,
    LoadMods
}