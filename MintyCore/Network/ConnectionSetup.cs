using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MintyCore.Modding;
using MintyCore.Utils;

namespace MintyCore.Network
{
    
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

        public void Deserialize(DataReader reader)
        {
            PlayerId = reader.GetULong();
            PlayerName = reader.GetString();

            int modCount = reader.GetInt();
            var mods = new (string modId, ModVersion version)[modCount];
                
            for (int i = 0; i < modCount; i++)
            {
                mods[i].modId = reader.GetString();
                mods[i].version = ModVersion.Deserialize(reader);
            }

            AvailableMods = mods;
        }
    }

    internal struct PlayerConnected
    {
        public ushort PlayerGameId;
        
        public void Serialize(DataWriter writer)
        {
            writer.Put(PlayerGameId);
        }

        public void Deserialize(DataReader reader)
        {
            PlayerGameId = reader.GetUShort();
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
            int modCount = Mods.Count();
            int modIDsCount = ModIDs.Count;
            int categoryIDsCount = CategoryIDs.Count;
            int objectIDsCount = CategoryIDs.Count;
            
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

        public void Deserialize(DataReader reader)
        {
            int modCount = reader.GetInt();
            int modIDsCount = reader.GetInt();
            int categoryIDsCount = reader.GetInt();
            int objectIDsCount = reader.GetInt();

            var mods = new (string modId, ModVersion modVersion)[modCount];
            var modIds = new Dictionary<ushort, string>(modIDsCount);
            var categoryIds = new Dictionary<ushort, string>(categoryIDsCount);
            var objectIds = new Dictionary<Identification, string>(objectIDsCount);

            for (int i = 0; i < modCount; i++)
            {
                mods[i] = (reader.GetString(), ModVersion.Deserialize(reader));
            }

            for (int i = 0; i < modIDsCount; i++)
            {
                modIds.Add(reader.GetUShort(), reader.GetString());
            }
            
            for (int i = 0; i < categoryIDsCount; i++)
            {
                categoryIds.Add(reader.GetUShort(), reader.GetString());
            }
            
            for (int i = 0; i < objectIDsCount; i++)
            {
                objectIds.Add(Identification.Deserialize(reader),reader.GetString());
            }

            Mods = mods;
            ModIDs = new ReadOnlyDictionary<ushort, string>(modIds);
            CategoryIDs = new ReadOnlyDictionary<ushort, string>(categoryIds);
            ObjectIDs = new ReadOnlyDictionary<Identification, string>(objectIds);
        }
    }
    
    

    internal enum ConnectionSetupMessageType
    {
        INVALID = Constants.InvalidId,
        PLAYER_INFORMATION,
        PLAYER_CONNECTED,
        LOAD_MODS
    }
}