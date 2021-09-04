using MintyCore.Utils;

namespace MintyCore.Network
{
    public class ConnectionSetup
    {
        
    }

    public struct PlayerInformation
    {
        public string PlayerName;
        public ulong PlayerId;
        
        public void Serialize(DataWriter writer)
        {
            writer.Put(PlayerId);
            writer.Put(PlayerName);
        }

        public void Deserialize(DataReader reader)
        {
            PlayerId = reader.GetULong();
            PlayerName = reader.GetString();
        }
    }

    public struct PlayerConnected
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

    public enum ConnectionSetupMessageType
    {
        INVALID = Constants.InvalidId,
        PLAYER_INFORMATION,
        PLAYER_CONNECTED
    }
}