using System;
using System.Security.Cryptography;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.ConnectionSetup.Encryption;

/*
 * This state is responsible for the encryption setup of the connection.
 * 0. Client and server both generates their dh parameters
 * 1. Server sends public key to client
 * 2. Client calculates aes key and sends its public key to server
 * 3. Server calculates aes key and sends enable encryption message to client
 * 4. Client acknowledges the enable encryption message, by sending enable encryption message to server
 * 5. Client enables encryption after sending the enable encryption message
 * 6. Server enables encryption after receiving the enable encryption message
 * 7. Done
 */

[RegisterConnectionSetupState("encryption")]
internal class EncryptionState(INetworkHandler networkHandler) : ISetupState
{
    public static Identification[] ExecuteAfter => [];
    public static Identification[] ExecuteBefore => [];
    public static bool Exclusive => true;

    public int ConnectionId { get; set; }
    public bool IsServerSide { get; set; }
    public bool IsFinished { get; private set; }

    private ECDiffieHellman? _ecdh;
    private byte[]? _publicKey;
    private byte[]? _aesKey;


    public void Start()
    {
        _ecdh = ECDiffieHellman.Create();
        _publicKey = _ecdh.PublicKey.ExportSubjectPublicKeyInfo();
        if (!IsServerSide) return;

        using var exchange = networkHandler.CreateMessage<DhExchange>();
        exchange.PublicKey = _publicKey;
        exchange.Send(ConnectionId);
    }

    public void CalculateAesKey(byte[] otherPublicKey)
    {
        if (_ecdh is null)
        {
            throw new InvalidOperationException("ECDH is not initialized");
        }

        var otherEcdh = ECDiffieHellman.Create();
        otherEcdh.ImportSubjectPublicKeyInfo(otherPublicKey, out _);

        _aesKey = _ecdh.DeriveKeyFromHash(otherEcdh.PublicKey, HashAlgorithmName.SHA256);

        if (IsServerSide)
        {
            using var enableEncryption = networkHandler.CreateMessage<EnableEncryption>();
            enableEncryption.Send(ConnectionId);
        }
        else
        {
            using var dhExchange = networkHandler.CreateMessage<DhExchange>();
            dhExchange.PublicKey = _publicKey;
            dhExchange.SendToServer();
        }
    }

    public void EnableAesEncryption()
    {
        if (_aesKey is null)
        {
            throw new InvalidOperationException("AES key is not calculated");
        }

        if (IsServerSide)
        {
            networkHandler.Server?.SetEncryption(ConnectionId, _aesKey);
        }
        else
        {
            using var enableEncryptionMessage = networkHandler.CreateMessage<EnableEncryption>();
            enableEncryptionMessage.SendToServer();
            
            networkHandler.Client?.SetEncryption(_aesKey);
        }

        IsFinished = true;
    }

    public void Process()
    {
    }

    public byte[] GetPublicKey()
    {
        if (_publicKey is null)
        {
            throw new InvalidOperationException("Public key is not set");
        }

        return _publicKey;
    }
}