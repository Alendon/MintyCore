using System;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using JetBrains.Annotations;
using LiteNetLib.Layers;

namespace MintyCore.Network;

/// <inheritdoc />
public class AesEncryptionLayer : PacketLayerBase
{
    public const int KeySize = 256;
    public const int BlockSize = 128;
    public const int KeySizeInBytes = KeySize / 8;
    public const int BlockSizeInBytes = BlockSize / 8;

    private readonly ConcurrentDictionary<IPEndPoint, byte[]> _encryptionKeys = new();

    /// <inheritdoc />
    public AesEncryptionLayer() : base(BlockSizeInBytes * 2)
    {
    }

    /// <inheritdoc />
    public override void ProcessInboundPacket(ref IPEndPoint endPoint, ref byte[] data, ref int length)
    {
        if (!_encryptionKeys.TryGetValue(endPoint, out var key)) return;

        using var aes = CreateAes(key, data.AsSpan(0, BlockSizeInBytes));

        var decryptor = aes.CreateDecryptor();

        data = decryptor.TransformFinalBlock(data, BlockSizeInBytes, length - BlockSizeInBytes);
        length = data.Length;
    }

    /// <inheritdoc />
    public override void ProcessOutBoundPacket(ref IPEndPoint endPoint, ref byte[] data, ref int offset, ref int length)
    {
        if (!_encryptionKeys.TryGetValue(endPoint, out var key)) return;

        using var aes = CreateAes(key);

        var encryptor = aes.CreateEncryptor();

        int currentWrite = aes.IV.Length;

        var newOutput = new byte[length + aes.IV.Length];
        aes.IV.AsSpan().CopyTo(newOutput);

        var transformed = encryptor.TransformFinalBlock(data, offset, length - offset);
        transformed.CopyTo(newOutput.AsSpan()[currentWrite..]);
        
        data = newOutput;
        offset = 0;
        length = newOutput.Length;
    }

    [MustDisposeResource]
    private Aes CreateAes(byte[] key, Span<byte> iv = default)
    {
        var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        if (iv.Length != 0)
        {
            iv.CopyTo(aes.IV);
        }

        return aes;
    }

    public void AddEndpointEncryption(IPEndPoint endpoint, byte[] key)
    {
        if (key.Length != KeySizeInBytes)
            throw new ArgumentException($"Key must be {KeySizeInBytes} bytes long", nameof(key));
        
        _encryptionKeys.AddOrUpdate(endpoint, key, (_, _) => key);
    }

    public void RemoveEndpointEncryption(IPEndPoint endpoint)
    {
        _encryptionKeys.TryRemove(endpoint, out _);
    }
}