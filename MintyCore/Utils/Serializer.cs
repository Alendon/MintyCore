﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ENet;
using JetBrains.Annotations;
using MintyCore.Utils.Maths;

namespace MintyCore.Utils;

/// <summary>
///     DataReader class used to deserialize data from byte arrays
/// </summary>
[PublicAPI]
public unsafe class DataReader : IDisposable
{
    private Region _currentRegion;

    /// <summary>
    ///     Create a new <see cref="DataReader" />
    /// </summary>
    public DataReader(byte[] source)
    {
        //Initialize Stub
        _currentRegion = Region.GetRegion(0, source.Length, null, null, 0);
        Buffer = source;
        Position = 0;

        _currentRegion = DeserializeRegion();
    }

    /// <summary>
    ///     Create a new <see cref="DataReader" />
    /// </summary>
    public DataReader(byte[] source, int position)
    {
        //Initialize Stub
        _currentRegion = Region.GetRegion(0, source.Length, null, null, 0);
        Buffer = source;
        Position = position;

        _currentRegion = DeserializeRegion();
    }

    /// <summary>
    ///     Create a new <see cref="DataReader" /> without copying the data
    /// </summary>
    public DataReader(IntPtr data, int length, int position = 0)
    {
        //Initialize Stub
        _currentRegion = Region.GetRegion(0, length, null, null, 0);

        _memoryOwner = MemoryPool<byte>.Shared.Rent(length);
        Buffer = _memoryOwner.Memory;

        var dataSpan = new Span<byte>(data.ToPointer(), length);
        var bufferSpan = Buffer.Span;
        dataSpan.CopyTo(bufferSpan);

        Position = position;

        _currentRegion = DeserializeRegion();
    }

    internal DataReader(Packet packet)
    {
        //Initialize Stub
        _currentRegion = Region.GetRegion(0, packet.Length, null, null, 0);

        if (!packet.IsSet)
        {
            Buffer = Array.Empty<byte>();
            Position = 0;
            return;
        }

        _memoryOwner = MemoryPool<byte>.Shared.Rent(packet.Length);
        Buffer = _memoryOwner.Memory;

        var packetSpan = new Span<byte>(packet.Data.ToPointer(), packet.Length);
        var bufferSpan = Buffer.Span;
        packetSpan.CopyTo(bufferSpan);

        Position = 0;

        _currentRegion = DeserializeRegion();
    }

    /// <summary>
    ///     Buffer of the reader
    /// </summary>
    public Memory<byte> Buffer { get; private set; }

    private IMemoryOwner<byte>? _memoryOwner;

    /// <summary>
    ///     Size of the reader
    /// </summary>
    public int DataSize => Buffer.Length;

    /// <summary>
    ///     Current Position of the reader
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    ///     Check if the end of the data is reached
    /// </summary>
    public bool EndOfData => Position >= DataSize;

    /// <summary>
    ///     Get the available bytes left
    /// </summary>
    public int AvailableBytes => _currentRegion.Start + _currentRegion.Length - Position;

    /// <summary>
    ///     Enter into a region
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void EnterRegion()
    {
        var nextRegion = _currentRegion.NextRegion();
        _currentRegion = nextRegion ?? throw new MintyCoreException("No region available to enter");
        Position = _currentRegion.Start;
    }

    /// <summary>
    ///     Exit from a region
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExitRegion()
    {
        _currentRegion = _currentRegion.ParentRegion ?? throw new MintyCoreException("Cannot leave root region");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private Region DeserializeRegion()
    {
        if (!TryGetInt(out var regionPosition))
            Fail();

        var startPosition = Position;

        Position = regionPosition;

        if (!Region.Deserialize(this, out var result)) Fail();

        Position = startPosition;
        return result;


        [DoesNotReturn]
        void Fail()
        {
            throw new MintyCoreException("Failed to deserialize regions");
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private bool CheckAccess(int dataSize)
    {
        return AvailableBytes >= dataSize;
    }

    /// <summary>
    ///     Offsets the internal position by the given byteCount
    /// </summary>
    /// <param name="byteCount">Count of bytes to offset</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Offset(int byteCount)
    {
        if (byteCount < 0)
            throw new MintyCoreException("Offset cannot be negative");
        Position += byteCount;
    }

    #region TryGetMethods

    /// <summary>
    ///     Try deserialize a <see cref="byte" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetByte(out byte result)
    {
        if (CheckAccess(sizeof(byte)))
        {
            result = FastBitConverter.Read<byte>(Buffer, Position);
            Position += sizeof(byte);
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="sbyte" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetSByte(out sbyte result)
    {
        if (CheckAccess(sizeof(sbyte)))
        {
            result = FastBitConverter.Read<sbyte>(Buffer, Position);
            Position += sizeof(sbyte);
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="short" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetShort(out short result)
    {
        if (CheckAccess(sizeof(short)))
        {
            result = FastBitConverter.Read<short>(Buffer, Position);
            Position += sizeof(short);
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="ushort" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetUShort(out ushort result)
    {
        if (CheckAccess(sizeof(ushort)))
        {
            result = FastBitConverter.Read<ushort>(Buffer, Position);
            Position += sizeof(ushort);
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="int" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetInt(out int result)
    {
        if (CheckAccess(sizeof(int)))
        {
            result = FastBitConverter.Read<int>(Buffer, Position);
            Position += sizeof(int);
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="uint" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetUInt(out uint result)
    {
        if (CheckAccess(sizeof(uint)))
        {
            result = FastBitConverter.Read<uint>(Buffer, Position);
            Position += sizeof(uint);
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="long" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetLong(out long result)
    {
        if (CheckAccess(sizeof(long)))
        {
            result = FastBitConverter.Read<long>(Buffer, Position);
            Position += sizeof(long);
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="ulong" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetULong(out ulong result)
    {
        if (CheckAccess(sizeof(ulong)))
        {
            result = FastBitConverter.Read<ulong>(Buffer, Position);
            Position += sizeof(ulong);
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="float" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetFloat(out float result)
    {
        if (CheckAccess(sizeof(float)))
        {
            result = FastBitConverter.Read<float>(Buffer, Position);
            Position += sizeof(float);
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="double" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetDouble(out double result)
    {
        if (CheckAccess(sizeof(double)))
        {
            result = FastBitConverter.Read<double>(Buffer, Position);
            Position += sizeof(double);
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try to deserialize a <see cref="string" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetString(out string result)
    {
        var currentPosition = Position;
        if (!TryGetInt(out var bytesCount))
        {
            Position = currentPosition;
            result = string.Empty;
            return false;
        }

        //Check if the size of the string is valid
        if (bytesCount <= 0)
        {
            Position = currentPosition;
            result = string.Empty;
            return false;
        }

        if (!CheckAccess(bytesCount))
        {
            Position = currentPosition;
            result = string.Empty;
            return false;
        }

        result = Encoding.UTF8.GetString(Buffer.Span.Slice(Position, bytesCount));
        Position += bytesCount;
        return true;
    }

    /// <summary>
    ///     Try deserialize a <see cref="string" /> array
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetStringArray(out string[] result)
    {
        var startPosition = Position;

        if (!TryGetUShort(out var size))
        {
            result = Array.Empty<string>();
            Position = startPosition;
            return false;
        }

        result = new string[size];
        for (var i = 0; i < size; i++)
            if (!TryGetString(out result[i]))
            {
                result = Array.Empty<string>();
                Position = startPosition;
                return false;
            }

        return true;
    }

    /// <summary>
    ///     Try to deserialize a <see cref="Vector2" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetVector2(out Vector2 result)
    {
        if (CheckAccess(sizeof(Vector2)))
        {
            result = default;

            ref var current = ref Unsafe.As<Vector2, float>(ref result);
            var size = sizeof(Vector2) / sizeof(float);

            for (var i = 0; i < size; i++, current = ref Unsafe.Add(ref current, 1))
                current = FastBitConverter.Read<float>(Buffer, Position + i * sizeof(float));

            Position += sizeof(Vector2);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    ///     Try to deserialize a <see cref="Vector3" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetVector3(out Vector3 result)
    {
        if (CheckAccess(sizeof(Vector3)))
        {
            result = default;

            ref var current = ref Unsafe.As<Vector3, float>(ref result);
            var size = sizeof(Vector3) / sizeof(float);

            for (var i = 0; i < size; i++, current = ref Unsafe.Add(ref current, 1))
                current = FastBitConverter.Read<float>(Buffer, Position + i * sizeof(float));

            Position += sizeof(Vector3);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    ///     Try to deserialize a <see cref="Vector4" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetVector4(out Vector4 result)
    {
        if (CheckAccess(sizeof(Vector4)))
        {
            result = default;

            ref var current = ref Unsafe.As<Vector4, float>(ref result);
            var size = sizeof(Vector4) / sizeof(float);

            for (var i = 0; i < size; i++, current = ref Unsafe.Add(ref current, 1))
                current = FastBitConverter.Read<float>(Buffer, Position + i * sizeof(float));

            Position += sizeof(Vector4);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    ///     Try to deserialize a <see cref="Quaternion" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetQuaternion(out Quaternion result)
    {
        if (CheckAccess(sizeof(Quaternion)))
        {
            result = default;

            ref var current = ref Unsafe.As<Quaternion, float>(ref result);
            var size = sizeof(Quaternion) / sizeof(float);

            for (var i = 0; i < size; i++, current = ref Unsafe.Add(ref current, 1))
                current = FastBitConverter.Read<float>(Buffer, Position + i * sizeof(float));

            Position += sizeof(Quaternion);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    ///     Try to deserialize a <see cref="Matrix4x4" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetMatrix4X4(out Matrix4x4 result)
    {
        if (CheckAccess(sizeof(Matrix4x4)))
        {
            result = default;

            ref var current = ref Unsafe.As<Matrix4x4, float>(ref result);
            var size = sizeof(Matrix4x4) / sizeof(float);

            for (var i = 0; i < size; i++, current = ref Unsafe.Add(ref current, 1))
                current = FastBitConverter.Read<float>(Buffer, Position + i * sizeof(float));

            Position += sizeof(Matrix4x4);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    ///     Try to deserialize a <see cref="bool" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetBool(out bool result)
    {
        if (CheckAccess(sizeof(byte)))
        {
            result = FastBitConverter.Read<byte>(Buffer, Position) != 0;
            Position += sizeof(byte);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Try deserialize a <see cref="Version"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetVersion([MaybeNullWhen(false)] out Version version)
    {
        version = null;

        var success = TryGetInt(out var major);
        success &= TryGetInt(out var minor);
        success &= TryGetInt(out var build);
        success &= TryGetInt(out var revision);


        if (success)
        {
            version = new Version(
                major == -1 ? 0 : major,
                minor == -1 ? 0 : minor,
                build == -1 ? 0 : build,
                revision == -1 ? 0 : revision);
        }

        return success;
    }

    #endregion

    private bool _disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        GC.SuppressFinalize(this);
        DisposeCore();
    }

    private void DisposeCore()
    {
        _memoryOwner?.Dispose();
        _memoryOwner = null;
        Buffer = Memory<byte>.Empty;
        _disposed = true;
        while (_currentRegion.ParentRegion is not null) _currentRegion = _currentRegion.ParentRegion;

        Region.ReturnRegionRecursive(_currentRegion);
    }

    /// <summary>
    /// Should never be called, always use dispose. Only implemented to assure that the memory is freed.
    /// </summary>
    ~DataReader()
    {
        DisposeCore();
    }
}

/// <summary>
///     Serialize Data to a byte array
/// </summary>
[PublicAPI]
public unsafe class DataWriter : IDisposable
{
    private Region _currentRegion;
    private Memory<byte> _internalBuffer;
    private IMemoryOwner<byte> _memoryOwner;


    private bool _regionAppliedToBuffer;
    private readonly ValueRef<int> _regionSerializationStart;

    private readonly Region _rootRegion;

    /// <summary>
    ///     Constructor
    /// </summary>
    public DataWriter()
    {
        //Initialize stub
        _rootRegion = _currentRegion = Region.GetRegion(0, null, null);
        _memoryOwner = MemoryPool<byte>.Shared.Rent(64);
        _internalBuffer = _memoryOwner.Memory;
        Position = 0;
        _regionSerializationStart = AddValueRef<int>();

        Region.ReturnRegionRecursive(_rootRegion);
        _rootRegion = _currentRegion = Region.GetRegion(Position, null, "root");
    }

    /// <summary>
    ///     Current position
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    ///     Get the length of the writer
    /// </summary>
    public int Length => Position;

    /// <summary>
    ///     Construct a buffer which can be deserialized with <see cref="DataReader" />
    ///     This instance will be invalidated afterwards
    /// </summary>
    public Span<byte> ConstructBuffer()
    {
        _rootRegion.Length = Position - _rootRegion.Start;

        _regionSerializationStart.SetValue(Position);

        _rootRegion.Serialize(this);
        _regionAppliedToBuffer = true;
        return _internalBuffer.Span[..Length];
    }

    /// <summary>
    ///     Offset the location of the writer
    /// </summary>
    /// <param name="offset"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void AddOffset(int offset)
    {
        Position += offset;
    }

    /// <summary>
    ///     Reset the <see cref="DataWriter" />. This will only move the Location of the writer to 0
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Reset()
    {
        Position = 0;
    }

    /// <summary>
    ///     Check if the data at the given position is accessible
    /// </summary>
    /// <param name="pos"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void CheckData(int pos)
    {
        if (_regionAppliedToBuffer)
            throw new MintyCoreException("Accessing the serializer after buffer construction is forbidden");
        ResizeIfNeed(pos);
    }

    /// <summary>
    ///     Enter into a new region
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void EnterRegion(string? regionName = null)
    {
        if (_regionAppliedToBuffer)
            throw new MintyCoreException("Accessing the serializer after buffer construction is forbidden");

        var region = Region.GetRegion(Position, _currentRegion, regionName);

        _currentRegion.AddRegion(region);
        _currentRegion = region;
    }

    /// <summary>
    ///     Exit a region
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExitRegion()
    {
        if (_regionAppliedToBuffer)
            throw new MintyCoreException("Accessing the serializer after buffer construction is forbidden");

        _currentRegion.Length = Position - _currentRegion.Start;
        _currentRegion = _currentRegion.ParentRegion ??
                         throw new MintyCoreException("Exiting the root region is forbidden");
    }

    /// <summary>
    ///     Resize if the position is out of bounds
    /// </summary>
    /// <param name="posCompare"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ResizeIfNeed(int posCompare)
    {
        var len = _internalBuffer.Length;
        if (len > posCompare) return;
        if (_disposed) throw new MintyCoreException("Writer is disposed");
        while (len <= posCompare)
        {
            len += 1;
            len *= 2;
        }

        len = MathHelper.CeilPower2(len);
        var newBufferOwner = MemoryPool<byte>.Shared.Rent(len);
        var newBuffer = newBufferOwner.Memory;

        var newBufferSpan = newBuffer.Span;
        var oldBufferSpan = _internalBuffer.Span;
        oldBufferSpan.CopyTo(newBufferSpan);

        _memoryOwner.Dispose();
        _memoryOwner = newBufferOwner;
        _internalBuffer = newBuffer;
    }

    /// <summary>
    ///     Serialize a <see cref="float" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(float value)
    {
        CheckData(Position + sizeof(float));
        FastBitConverter.Write(_internalBuffer, Position, value);
        Position += sizeof(float);
    }

    /// <summary>
    ///     Serialize a <see cref="double" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(double value)
    {
        CheckData(Position + sizeof(decimal));
        FastBitConverter.Write(_internalBuffer, Position, value);
        Position += sizeof(decimal);
    }

    /// <summary>
    ///     Serialize a <see cref="long" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(long value)
    {
        CheckData(Position + sizeof(long));
        FastBitConverter.Write(_internalBuffer, Position, value);
        Position += sizeof(long);
    }

    /// <summary>
    ///     Serialize a <see cref="ulong" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(ulong value)
    {
        CheckData(Position + sizeof(ulong));
        FastBitConverter.Write(_internalBuffer, Position, value);
        Position += sizeof(ulong);
    }

    /// <summary>
    ///     Serialize a <see cref="int" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(int value)
    {
        CheckData(Position + sizeof(int));
        FastBitConverter.Write(_internalBuffer, Position, value);
        Position += sizeof(int);
    }

    /// <summary>
    ///     Serialize a <see cref="uint" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(uint value)
    {
        CheckData(Position + sizeof(uint));
        FastBitConverter.Write(_internalBuffer, Position, value);
        Position += sizeof(uint);
    }

    /// <summary>
    ///     Serialize a <see cref="char" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(char value)
    {
        CheckData(Position + sizeof(char));
        FastBitConverter.Write(_internalBuffer, Position, value);
        Position += sizeof(char);
    }

    /// <summary>
    ///     Serialize a <see cref="ushort" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(ushort value)
    {
        CheckData(Position + sizeof(ushort));
        FastBitConverter.Write(_internalBuffer, Position, value);
        Position += sizeof(ushort);
    }

    /// <summary>
    ///     Serialize a <see cref="short" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(short value)
    {
        CheckData(Position + sizeof(short));
        FastBitConverter.Write(_internalBuffer, Position, value);
        Position += sizeof(short);
    }

    /// <summary>
    ///     Serialize a <see cref="sbyte" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(sbyte value)
    {
        CheckData(Position + sizeof(sbyte));
        Unsafe.As<byte, sbyte>(ref _internalBuffer.Span[Position]) = value;
        Position += sizeof(sbyte);
    }

    /// <summary>
    ///     Serialize a <see cref="byte" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(byte value)
    {
        CheckData(Position + sizeof(byte));
        _internalBuffer.Span[Position] = value;
        Position += sizeof(byte);
    }

    /// <summary>
    ///     Serialize a <see cref="IPEndPoint" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(IPEndPoint endPoint)
    {
        Put(endPoint.Address.ToString());
        Put(endPoint.Port);
    }

    /// <summary>
    ///     Serialize a <see cref="string" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Put(0);
            return;
        }

        //put bytes count
        var bytesCount = Encoding.UTF8.GetByteCount(value);
        ResizeIfNeed(Position + bytesCount + 4);
        Put(bytesCount);

        //put string
        Encoding.UTF8.GetBytes(value.AsSpan(), _internalBuffer.Span[Position..]);

        Position += bytesCount;
    }

    /// <summary>
    ///     Serialize a <see cref="Vector2" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(Vector2 value)
    {
        CheckData(Position + sizeof(Vector2));

        ref var current = ref Unsafe.As<Vector2, float>(ref value);
        var size = sizeof(Vector2) / sizeof(float);

        for (var i = 0; i < size; i++, current = ref Unsafe.Add(ref current, 1))
            FastBitConverter.Write(_internalBuffer, Position + i * sizeof(float), current);

        Position += sizeof(Vector2);
    }

    /// <summary>
    ///     Serialize a <see cref="Vector3" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(Vector3 value)
    {
        CheckData(Position + sizeof(Vector3));

        ref var current = ref Unsafe.As<Vector3, float>(ref value);
        var size = sizeof(Vector3) / sizeof(float);

        for (var i = 0; i < size; i++, current = ref Unsafe.Add(ref current, 1))
            FastBitConverter.Write(_internalBuffer, Position + i * sizeof(float), current);

        Position += sizeof(Vector3);
    }

    /// <summary>
    ///     Serialize a <see cref="Vector4" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(Vector4 value)
    {
        CheckData(Position + sizeof(Vector4));

        ref var current = ref Unsafe.As<Vector4, float>(ref value);
        var size = sizeof(Vector4) / sizeof(float);

        for (var i = 0; i < size; i++, current = ref Unsafe.Add(ref current, 1))
            FastBitConverter.Write(_internalBuffer, Position + i * sizeof(float), current);

        Position += sizeof(Vector4);
    }

    /// <summary>
    ///     Serialize a <see cref="Quaternion" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(Quaternion value)
    {
        CheckData(Position + sizeof(Quaternion));

        ref var current = ref Unsafe.As<Quaternion, float>(ref value);
        var size = sizeof(Quaternion) / sizeof(float);

        for (var i = 0; i < size; i++, current = ref Unsafe.Add(ref current, 1))
            FastBitConverter.Write(_internalBuffer, Position + i * sizeof(float), current);

        Position += sizeof(Quaternion);
    }

    /// <summary>
    ///     Serialize a <see cref="Matrix4x4" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(Matrix4x4 value)
    {
        CheckData(Position + sizeof(Matrix4x4));

        ref var current = ref Unsafe.As<Matrix4x4, float>(ref value);
        var size = sizeof(Matrix4x4) / sizeof(float);

        for (var i = 0; i < size; i++, current = ref Unsafe.Add(ref current, 1))
            FastBitConverter.Write(_internalBuffer, Position + i * sizeof(float), current);

        Position += sizeof(Matrix4x4);
    }

    /// <summary>
    ///     Serialize a <see cref="bool" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(bool value)
    {
        if (value)
            Put((byte)1);
        else
            Put((byte)0);
    }

    /// <summary>
    /// Serialize a <see cref="Version"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(Version version)
    {
        Put(version.Major);
        Put(version.Minor);
        Put(version.Build);
        Put(version.Revision);
    }

    /// <summary>
    ///     Add a reference to the current position in the writer to write to later
    /// </summary>
    /// <typeparam name="T">Type to write later, must be numeric</typeparam>
    /// <exception cref="MintyCoreException">If T is not numeric</exception>
    /// <returns>A "reference" to write later on</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public ValueRef<T> AddValueRef<T>() where T : unmanaged
    {
        if (!IsNumeric())
            throw new MintyCoreException("Value refs are only valid for numeric types");

        CheckData(Position + sizeof(T));
        var reference = new ValueRef<T>(this, Position);

        //Zero the data
        Unsafe.As<byte, T>(ref _internalBuffer.Span[Position]) = default;
        Position += sizeof(T);
        return reference;

        bool IsNumeric()
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    ///     Stores a "reference" to a variable inside a <see cref="DataWriter" />
    /// </summary>
    /// <typeparam name="T">Type of the variable to reference</typeparam>
    public readonly struct ValueRef<T> where T : unmanaged
    {
        private readonly DataWriter? _parent;
        private readonly int _dataPosition;

        internal ValueRef(DataWriter parent, int dataPosition)
        {
            _parent = parent;
            _dataPosition = dataPosition;
        }

        /// <summary>
        ///     Set the value of the referenced variable
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(T value)
        {
            if (_parent is null)
                throw new MintyCoreException("The internal data writer is null");
            FastBitConverter.Write(_parent._internalBuffer, _dataPosition, value);
        }
    }

    private bool _disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        DisposeCore();

        GC.SuppressFinalize(this);
    }

    private void DisposeCore()
    {
        _memoryOwner.Dispose();
        _internalBuffer = Memory<byte>.Empty;
        _disposed = true;
        Region.ReturnRegionRecursive(_rootRegion);
    }

    ~DataWriter()
    {
        DisposeCore();
    }
}

internal class Region
{
    private static readonly Queue<List<Region>> _regionListPool = new();

    private static List<Region> GetRegionList()
    {
        lock (_regionListPool)
        {
            return _regionListPool.Count > 0 ? _regionListPool.Dequeue() : new List<Region>();
        }
    }

    private static void ReturnRegionList(List<Region> list)
    {
        lock (_regionListPool)
        {
            list.Clear();
            _regionListPool.Enqueue(list);
        }
    }

    private static readonly Queue<Region> _regionPool = new();

    public static Region GetRegion(int start, Region? parent, string? name)
    {
        Region region;

        lock (_regionPool)
        {
            region = _regionPool.Count > 0 ? _regionPool.Dequeue() : new Region();
        }

        region.Start = start;
        region.ParentRegion = parent;
        region._name = name;

        return region;
    }

    public static Region GetRegion(int start, int length, Region? parent, string? name, int childCount)
    {
        Region region;

        lock (_regionPool)
        {
            region = _regionPool.Count > 0 ? _regionPool.Dequeue() : new Region();
        }

        region.Start = start;
        region.ParentRegion = parent;
        region._name = name;
        region.ChildRegions.EnsureCapacity(childCount);
        region.Length = length;

        return region;
    }

    public static void ReturnRegionRecursive(Region region)
    {
        lock (_regionPool)
        {
            var currentRegion = region;
            while (currentRegion is not null)
            {
                //If the current region is a leaf region, return it to the pool
                if (currentRegion.ChildRegions.Count == 0)
                {
                    ReturnRegionList(currentRegion.ChildRegions);

                    var regionCopy = currentRegion;

                    _regionPool.Enqueue(currentRegion);
                    currentRegion = currentRegion.ParentRegion;

                    regionCopy._childRegions = null;
                    regionCopy.ParentRegion = null;
                    regionCopy._name = null;
                    regionCopy.Length = 0;
                    regionCopy.Start = 0;
                    regionCopy._currentRegion = 0;

                    continue;
                }

                //If the current region is not a leaf region, traverse to the child region
                //Use the last region in the list to prevent moving internal data
                var newRegion = currentRegion.ChildRegions[^1];
                currentRegion.ChildRegions.RemoveAt(currentRegion.ChildRegions.Count - 1);
                currentRegion = newRegion;
            }
        }
    }

    private List<Region>? _childRegions;

    private List<Region> ChildRegions
    {
        get { return _childRegions ??= GetRegionList(); }
        [UsedImplicitly] set => _childRegions = value;
    }

    public Region? ParentRegion { get; private set; }
    public int Start { get; private set; }
    private int _currentRegion;
    public int Length;
    private string? _name;

    public void AddRegion(Region region)
    {
        ChildRegions.Add(region);
    }

    public Region? NextRegion()
    {
        if (_currentRegion >= ChildRegions.Count) return null;

        var result = ChildRegions[_currentRegion++];
        return result;
    }

    public void Serialize(DataWriter writer)
    {
        writer.Put(Start);
        writer.Put(Length);
        writer.Put(_name is not null);
        if (_name is not null)
            writer.Put(_name);

        writer.Put(ChildRegions.Count);
        foreach (var childRegion in ChildRegions) childRegion.Serialize(writer);
    }

    public static bool Deserialize(DataReader reader, [NotNullWhen(true)] out Region? region,
        Region? parentRegion = null)
    {
        region = null;

        string? name = null;

        if (!reader.TryGetInt(out var start) || !reader.TryGetInt(out var length) ||
            !reader.TryGetBool(out var hasName)) return false;
        if (hasName && !reader.TryGetString(out name)) return false;
        if (!reader.TryGetInt(out var childCount)) return false;

        region = GetRegion(start, length, parentRegion, name, childCount);

        for (var i = 0; i < childCount; i++)
        {
            if (!Deserialize(reader, out var child, region)) return false;

            region.ChildRegions.Add(child);
        }

        return true;
    }
}

internal static class FastBitConverter
{
    public static unsafe void Write<T>(Memory<byte> bytes, int startIndex, T value) where T : unmanaged
    {
        Unsafe.As<byte, T>(ref bytes.Span[startIndex]) = value;

        if (BitConverter.IsLittleEndian) return;
        bytes.Span.Slice(startIndex, sizeof(T)).Reverse();
    }

    public static unsafe T Read<T>(Memory<byte> bytes, int index) where T : unmanaged
    {
        if (!BitConverter.IsLittleEndian)
            //If this machine is using big endian we need to convert the data
            bytes.Span.Slice(index, sizeof(T)).Reverse();

        return Unsafe.As<byte, T>(ref bytes.Span[index]);
    }
}