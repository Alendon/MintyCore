using System;
using LiteNetLib;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.ConnectionSetup.Encryption;

[RegisterUnconnectedMessage("dh_exchange")]
internal class DhExchange(IConnectionSetupManager setupManager) : UnconnectedMessage
{
    public override Identification MessageId => MessageIDs.DhExchange;
    public override bool ReceiveMultiThreaded => false;
    public override DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;
    public override MagicHeader MagicSequence => MagicHeader.Create(NetworkUtils.OriginSpan, "DH"u8, 1);
    public byte[]? PublicKey { get; set; }

    public override void Serialize(DataWriter writer)
    {
        if (PublicKey is null)
        {
            throw new InvalidOperationException("Public key is not set");
        }
        
        writer.Put(PublicKey.Length);
        writer.Put(PublicKey.AsSpan());
    }

    public override bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetInt(out var keyLength)) return false;
        
        PublicKey = new byte[keyLength];
        if (!reader.TryFillByteSpan(PublicKey)) return false;

        var encryptionState = setupManager.GetStateForConnection<EncryptionState>(Sender);
        encryptionState.CalculateAesKey(PublicKey);
        
        return true;
    }

    public override void Clear()
    {
        PublicKey = null;
    }
}