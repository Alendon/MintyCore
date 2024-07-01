using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MintyCore.Utils;

public struct MagicHeader
{
    private ulong _value;
    public unsafe Span<byte> Value => new(Unsafe.AsPointer(ref _value), 8);
    
    public Span<byte> Origin => Value[..4];
    public Span<byte> Method => Value[4..6];
    public Span<byte> Version => Value[6..8];
    
    public string OriginString => Encoding.UTF8.GetString(Origin);
    public string MethodString => Encoding.UTF8.GetString(Method);
    public ushort VersionNumber => BinaryPrimitives.ReadUInt16LittleEndian(Version);

    public static MagicHeader Create(ReadOnlySpan<byte> value)
    {
        if (value.Length != 8)
        {
            throw new InvalidOperationException("Magic header must be 8 bytes long");
        }

        MagicHeader header = default;
        value.CopyTo(header.Value);
        return header;
    }

    public static MagicHeader Create(string origin, string method, ushort version)
    {
        //convert origin and method to utf8 reps
        Span<byte> originBytes = [0, 0, 0, 0];
        Span<byte> methodBytes = [0, 0];

        if (!Encoding.UTF8.TryGetBytes(origin, originBytes, out _))
            throw new InvalidOperationException("Failed to convert origin to utf8");

        if (!Encoding.UTF8.TryGetBytes(method, methodBytes, out _))
            throw new InvalidOperationException("Failed to convert method to utf8");

        return Create(originBytes, methodBytes, version);
    }

    public static MagicHeader Create(ReadOnlySpan<byte> origin, ReadOnlySpan<byte> method, ushort version)
    {
        Span<byte> versionBytes = [0, 0];
        BinaryPrimitives.WriteUInt16LittleEndian(versionBytes, version);

        return Create(origin, method, versionBytes);
    }

    public static MagicHeader Create(ReadOnlySpan<byte> origin, ReadOnlySpan<byte> method, ReadOnlySpan<byte> version)
    {
        if (origin.Length != 4)
            throw new InvalidOperationException("Origin must be 4 bytes long");
        if (origin.Length != 2)
            throw new InvalidOperationException("Method must be 2 bytes long");
        if (origin.Length != 2)
            throw new InvalidOperationException("Version must be 2 bytes long");

        MagicHeader header = default;
        origin.CopyTo(header.Value);
        method.CopyTo(header.Value[4..]);
        version.CopyTo(header.Value[6..]);

        return header;
    }

    public bool Equals(MagicHeader other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is MagicHeader other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public static bool operator ==(MagicHeader left, MagicHeader right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MagicHeader left, MagicHeader right)
    {
        return !left.Equals(right);
    }
}