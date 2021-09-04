using MintyCore.Utils;

namespace MintyCore.Network
{
    public enum MessageType
    {
        INVALID = Constants.InvalidId,
        CONNECTION_SETUP,
        ENGINE_MESSAGE,
        REGISTERED_MESSAGE        
    }
}