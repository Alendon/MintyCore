using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using LiteNetLib.Utils;
using MintyCore.Modding;
using MintyCore.Utils.Maths;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.BitConverter;

namespace MintyCore.Utils;

/// <summary>
///     DataReader class used to deserialize data from byte arrays
/// </summary>
[PublicAPI]
public unsafe class DataReader : IDisposable
{
    private Region _currentRegion;

    public FrozenDictionary<Identification, Identification>? IdMap { private get; init; }

    public DataReader(NetDataReader requestData, IModManager modManager) : this(requestData.GetRemainingBytesSpan(), modManager)
    {
        //TODO investigate if it's possible to avoid copying the data
    }

    /// <summary>
    ///     Create a new <see cref="DataReader" />
    /// </summary>
    public DataReader(ReadOnlySpan<byte> source, IModManager modManager)
    {
        if (source.Length < 8)
            throw new MintyCoreException("Data is too small to be a valid data reader");

        var magic = source[..8];
        MagicSequence = MagicHeader.Create(magic);
        source = source[8..];

        _memoryOwner = MemoryPool<byte>.Shared.Rent(source.Length);
        Buffer = _memoryOwner.Memory;
        source.CopyTo(Buffer.Span);

        _currentRegion = DeserializeRegion();
    }

    /// <summary>
    ///     Buffer of the reader
    /// </summary>
    public Memory<byte> Buffer { get; private set; }

    private ReadOnlySpan<byte> ReadSpan => Buffer.Span[Position..];

    /// <summary>
    /// Access magic sequence
    /// </summary>
    public MagicHeader MagicSequence { get; private init; }

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
            result = ReadSpan[0];
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
            var byteVal = ReadSpan[0];
            result = Unsafe.As<byte, sbyte>(ref byteVal);
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
            result = IsLittleEndian
                ? ReadInt16LittleEndian(ReadSpan)
                : ReadInt16BigEndian(ReadSpan);

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
            result = IsLittleEndian
                ? ReadUInt16LittleEndian(ReadSpan)
                : ReadUInt16BigEndian(ReadSpan);

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
            result = IsLittleEndian
                ? ReadInt32LittleEndian(ReadSpan)
                : ReadInt32BigEndian(ReadSpan);

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
            result = IsLittleEndian
                ? ReadUInt32LittleEndian(ReadSpan)
                : ReadUInt32BigEndian(ReadSpan);

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
            result = IsLittleEndian
                ? ReadInt64LittleEndian(ReadSpan)
                : ReadInt64BigEndian(ReadSpan);

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
            result = IsLittleEndian
                ? ReadUInt64LittleEndian(ReadSpan)
                : ReadUInt64BigEndian(ReadSpan);

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
            result = IsLittleEndian
                ? ReadSingleLittleEndian(ReadSpan)
                : ReadSingleBigEndian(ReadSpan);

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
            result = IsLittleEndian
                ? ReadDoubleLittleEndian(ReadSpan)
                : ReadDoubleBigEndian(ReadSpan);

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
            result = [];
            Position = startPosition;
            return false;
        }

        result = new string[size];
        for (var i = 0; i < size; i++)
            if (!TryGetString(out result[i]))
            {
                result = [];
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
            {
                current = IsLittleEndian
                    ? ReadSingleLittleEndian(Buffer.Span[(Position + i * sizeof(float))..])
                    : ReadSingleBigEndian(Buffer.Span[(Position + i * sizeof(float))..]);
            }

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
            {
                current = IsLittleEndian
                    ? ReadSingleLittleEndian(Buffer.Span[(Position + i * sizeof(float))..])
                    : ReadSingleBigEndian(Buffer.Span[(Position + i * sizeof(float))..]);
            }

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
            {
                current = IsLittleEndian
                    ? ReadSingleLittleEndian(Buffer.Span[(Position + i * sizeof(float))..])
                    : ReadSingleBigEndian(Buffer.Span[(Position + i * sizeof(float))..]);
            }

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
            {
                current = IsLittleEndian
                    ? ReadSingleLittleEndian(Buffer.Span[(Position + i * sizeof(float))..])
                    : ReadSingleBigEndian(Buffer.Span[(Position + i * sizeof(float))..]);
            }

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
            {
                current = IsLittleEndian
                    ? ReadSingleLittleEndian(Buffer.Span[(Position + i * sizeof(float))..])
                    : ReadSingleBigEndian(Buffer.Span[(Position + i * sizeof(float))..]);
            }

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
            result = ReadSpan[0] != 0;
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

    /// <summary>
    /// Try deserialize a <see cref="Identification"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetIdentification(out Identification id)
    {
        var successful = TryGetUShort(out var mod);
        successful &= TryGetUShort(out var category);
        successful &= TryGetUInt(out var @object);

        id = new Identification(mod, category, @object);

        if (IdMap is not null)
            id = IdMap[id];

        return successful;
    }

    public bool TryFillByteSpan(Span<byte> destination)
    {
        if (!CheckAccess(destination.Length)) return false;

        ReadSpan[..destination.Length].CopyTo(destination);
        Position += destination.Length;
        return true;
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

    private Span<byte> CurrentWrite => _internalBuffer.Span[Position..];


    private bool _regionAppliedToBuffer;
    private readonly ValueRef<int> _regionSerializationStart;

    private readonly Region _rootRegion;

    public FrozenDictionary<Identification, Identification>? IdMap { private get; init; }


    /// <summary>
    ///     DataWriter constructor
    /// </summary>
    /// <param name="magic">8 byte magic sequence, used for identifying content of the DataWriter</param>
    public DataWriter(MagicHeader magic)
    {
        _memoryOwner = MemoryPool<byte>.Shared.Rent(256);
        _internalBuffer = _memoryOwner.Memory;
        Position = 0;

        Put(magic.Value);
        _regionSerializationStart = AddValueRef<int>();
        Debug.Assert(Position == 8 + sizeof(int),
            $"Position after DataWriter construction has an unexpected value ({Position})");

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
    /// Access the magic sequence unsafely. The returned span might be invalidated if modifying DataWriter methods are called
    /// </summary>
    public Span<byte> MagicUnsafe => _internalBuffer.Span[..8];

    /// <summary>
    ///     Construct a buffer which can be deserialized with <see cref="DataReader" />
    ///     This instance will be invalidated afterwards
    /// </summary>
    public Span<byte> ConstructBuffer()
    {
        ApplyRootRegion();
        return _internalBuffer.Span[..Length];
    }

    private void ApplyRootRegion()
    {
        if (_regionAppliedToBuffer) return;

        _rootRegion.Length = Position - _rootRegion.Start;

        _regionSerializationStart.SetValue(Position);

        _rootRegion.Serialize(this);
        _regionAppliedToBuffer = true;
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
        WriteSingleLittleEndian(CurrentWrite, value);
        Position += sizeof(float);
    }

    /// <summary>
    ///     Serialize a <see cref="double" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(double value)
    {
        CheckData(Position + sizeof(decimal));
        WriteDoubleLittleEndian(CurrentWrite, value);
        Position += sizeof(decimal);
    }

    /// <summary>
    ///     Serialize a <see cref="long" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(long value)
    {
        CheckData(Position + sizeof(long));
        WriteInt64LittleEndian(CurrentWrite, value);
        Position += sizeof(long);
    }

    /// <summary>
    ///     Serialize a <see cref="ulong" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(ulong value)
    {
        CheckData(Position + sizeof(ulong));
        WriteUInt64LittleEndian(CurrentWrite, value);
        Position += sizeof(ulong);
    }

    /// <summary>
    ///     Serialize a <see cref="int" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(int value)
    {
        CheckData(Position + sizeof(int));
        WriteInt32LittleEndian(CurrentWrite, value);
        Position += sizeof(int);
    }

    /// <summary>
    ///     Serialize a <see cref="uint" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(uint value)
    {
        CheckData(Position + sizeof(uint));
        WriteUInt32LittleEndian(CurrentWrite, value);
        Position += sizeof(uint);
    }

    /// <summary>
    ///     Serialize a <see cref="ushort" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(ushort value)
    {
        CheckData(Position + sizeof(ushort));
        WriteUInt16LittleEndian(CurrentWrite, value);
        Position += sizeof(ushort);
    }

    /// <summary>
    ///     Serialize a <see cref="short" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(short value)
    {
        CheckData(Position + sizeof(short));
        WriteInt16LittleEndian(CurrentWrite, value);
        Position += sizeof(short);
    }

    /// <summary>
    ///     Serialize a <see cref="sbyte" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(sbyte value)
    {
        CheckData(Position + sizeof(sbyte));
        CurrentWrite[0] = Unsafe.As<sbyte, byte>(ref value);
        Position += sizeof(sbyte);
    }

    /// <summary>
    ///     Serialize a <see cref="byte" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(byte value)
    {
        CheckData(Position + sizeof(byte));
        CurrentWrite[0] = value;
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
        {
            WriteSingleLittleEndian(CurrentWrite[(i * sizeof(float))..], current);
        }

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
        {
            WriteSingleLittleEndian(CurrentWrite[(i * sizeof(float))..], current);
        }

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
        {
            WriteSingleLittleEndian(CurrentWrite[(i * sizeof(float))..], current);
        }

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
        {
            WriteSingleLittleEndian(CurrentWrite[(i * sizeof(float))..], current);
        }

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
        {
            WriteSingleLittleEndian(CurrentWrite[(i * sizeof(float))..], current);
        }

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
    /// Serialize a <see cref="Identification"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Put(Identification id)
    {
        if (IdMap is not null)
        {
            id = IdMap[id];
        }

        Put(id.Mod);
        Put(id.Category);
        Put(id.Object);
    }

    public void Put(ReadOnlySpan<byte> data)
    {
        CheckData(Position + data.Length);
        data.CopyTo(CurrentWrite);
        Position += data.Length;
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
        if (!TryGetConversion(out var m))
            throw new MintyCoreException($"{nameof(T)} is not supported as a value reference");

        CheckData(Position + sizeof(T));
        var reference = new ValueRef<T>(this, Position, m);

        //Zero the data
        Unsafe.As<byte, T>(ref _internalBuffer.Span[Position]) = default;
        Position += sizeof(T);
        return reference;

        bool TryGetConversion(out Action<int, T> conv)
        {
            conv = delegate { };
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Byte:
                    conv = (pos, val) => _internalBuffer.Span[pos] = Unsafe.As<T, byte>(ref val);
                    return true;
                case TypeCode.SByte:
                    conv = (pos, val) => _internalBuffer.Span[pos] = Unsafe.As<T, byte>(ref val);
                    return true;
                case TypeCode.UInt16:
                    conv = (pos, val) =>
                        WriteUInt16LittleEndian(_internalBuffer.Span[pos..], Unsafe.As<T, ushort>(ref val));
                    return true;
                case TypeCode.UInt32:
                    conv = (pos, val) =>
                        WriteUInt32LittleEndian(_internalBuffer.Span[pos..], Unsafe.As<T, uint>(ref val));
                    return true;
                case TypeCode.UInt64:
                    conv = (pos, val) =>
                        WriteUInt64LittleEndian(_internalBuffer.Span[pos..], Unsafe.As<T, ulong>(ref val));
                    return true;
                case TypeCode.Int16:
                    conv = (pos, val) =>
                        WriteInt16LittleEndian(_internalBuffer.Span[pos..], Unsafe.As<T, short>(ref val));
                    return true;
                case TypeCode.Int32:
                    conv = (pos, val) =>
                        WriteInt32LittleEndian(_internalBuffer.Span[pos..], Unsafe.As<T, int>(ref val));
                    return true;
                case TypeCode.Int64:
                    conv = (pos, val) =>
                        WriteInt64LittleEndian(_internalBuffer.Span[pos..], Unsafe.As<T, long>(ref val));
                    return true;
                case TypeCode.Double:
                    conv = (pos, val) =>
                        WriteDoubleLittleEndian(_internalBuffer.Span[pos..], Unsafe.As<T, double>(ref val));
                    return true;
                case TypeCode.Single:
                    conv = (pos, val) =>
                        WriteSingleLittleEndian(_internalBuffer.Span[pos..], Unsafe.As<T, float>(ref val));
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
        private readonly Action<int, T> _writeFunc;

        internal ValueRef(DataWriter parent, int dataPosition, Action<int, T> writeFunc)
        {
            _parent = parent;
            _dataPosition = dataPosition;
            _writeFunc = writeFunc;
        }

        /// <summary>
        ///     Set the value of the referenced variable
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(T value)
        {
            if (_parent is null)
                throw new MintyCoreException("The internal data writer is null");
            _writeFunc(_dataPosition, value);
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
            return _regionListPool.Count > 0 ? _regionListPool.Dequeue() : [];
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