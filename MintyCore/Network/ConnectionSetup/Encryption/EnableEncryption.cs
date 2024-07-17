using LiteNetLib;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.ConnectionSetup.Encryption;

[RegisterUnconnectedMessage("enable_encryption")]
public class EnableEncryption(IConnectionSetupManager setupManager) : UnconnectedMessage
{
    public override Identification MessageId { get; }
    public override bool ReceiveMultiThreaded { get; }
    public override DeliveryMethod DeliveryMethod { get; }
    public override void Serialize(DataWriter writer)
    {
        
    }

    public override bool Deserialize(DataReader reader)
    {
        var encryptionState = setupManager.GetStateForConnection<EncryptionState>(Sender);
    }

    public override void Clear()
    {
        throw new System.NotImplementedException();
    }

    public override MagicHeader MagicSequence { get; }
}