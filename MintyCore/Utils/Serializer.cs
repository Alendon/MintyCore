using System;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ENet;
using MintyCore.Utils.Maths;

namespace MintyCore.Utils;

//TODO CRITICAL "Obsolete" the DataReader.GetValue methods. They could potentially crash a game server just by connecting with a different version which has changes how the data is serialized

/// <summary>
///     DataReader class used to deserialize data from byte arrays
/// </summary>
public class DataReader
{
    /// <summary>
    ///     Create a new <see cref="DataReader" />
    /// </summary>
    public DataReader(byte[] source)
    {
        Buffer = source;
        Position = 0;
    }

    /// <summary>
    ///     Create a new <see cref="DataReader" />
    /// </summary>
    public DataReader(byte[] source, int position)
    {
        Buffer = source;
        Position = position;
    }

    /// <summary>
    ///     Create a new <see cref="DataReader" /> without copying the data
    /// </summary>
    public DataReader(IntPtr data, int length, int position = 0)
    {
        Buffer = new byte[length];

        Marshal.Copy(data, Buffer, position, length);

        Position = position;
    }

    internal DataReader(Packet packet)
    {
        if (!packet.IsSet)
        {
            Buffer = Array.Empty<byte>();
            Position = 0;
            return;
        }

        Buffer = new byte[packet.Length];

        Marshal.Copy(packet.Data, Buffer, 0, packet.Length);
        Position = 0;
    }

    /// <summary>
    ///     Buffer of the reader
    /// </summary>
    public byte[] Buffer { get; }

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
    public int AvailableBytes => DataSize - Position;

    /// <summary>
    ///     Check if the access at <paramref name="position" /> is valid
    /// </summary>
    /// <param name="position"></param>
    [Conditional("DEBUG")]
    private void CheckAccess(int position)
    {
        if (position >= Buffer.Length)
            throw new IndexOutOfRangeException($"{position} is out of range in the internal buffer");
    }

    /// <summary>
    /// Offsets the internal position by the given byteCount
    /// </summary>
    /// <param name="byteCount">Count of bytes to offset</param>
    public void Offset(int byteCount)
    {
        Logger.AssertAndThrow(byteCount >= 0, "Offset cannot be negative", "Utils");
        Position += byteCount;
    }

    #region GetMethods

    /// <summary>
    ///     Deserialize a <see cref="byte" />
    /// </summary>
    public byte GetByte()
    {
        CheckAccess(Position);
        var res = Buffer[Position];
        Position += 1;
        return res;
    }

    /// <summary>
    ///     Deserialize a <see cref="sbyte" />
    /// </summary>
    public sbyte GetSByte()
    {
        CheckAccess(Position);
        var res = Unsafe.As<byte, sbyte>(ref Buffer[Position]);
        Position += 1;
        return res;
    }

    /// <summary>
    ///     Deserialize a <see cref="ushort" />
    /// </summary>
    public ushort GetUShort()
    {
        CheckAccess(Position + 1);
        var result = FastBitConverter.Read<ushort>(Buffer, Position);
        Position += 2;
        return result;
    }

    /// <summary>
    ///     Deserialize a <see cref="short" />
    /// </summary>
    public short GetShort()
    {
        CheckAccess(Position + 1);
        var result = FastBitConverter.Read<short>(Buffer, Position);
        Position += 2;
        return result;
    }

    /// <summary>
    ///     Deserialize a <see cref="long" />
    /// </summary>
    public long GetLong()
    {
        CheckAccess(Position + 7);
        var result = FastBitConverter.Read<long>(Buffer, Position);
        Position += 8;
        return result;
    }

    /// <summary>
    ///     Deserialize a <see cref="ulong" />
    /// </summary>
    public ulong GetULong()
    {
        CheckAccess(Position + 7);
        var result = FastBitConverter.Read<ulong>(Buffer, Position);
        Position += 8;
        return result;
    }

    /// <summary>
    ///     Deserialize a <see cref="int" />
    /// </summary>
    public int GetInt()
    {
        CheckAccess(Position + 3);
        var result = FastBitConverter.Read<int>(Buffer, Position);
        Position += 4;
        return result;
    }

    /// <summary>
    ///     Deserialize a <see cref="uint" />
    /// </summary>
    public uint GetUInt()
    {
        CheckAccess(Position + 3);
        var result = FastBitConverter.Read<uint>(Buffer, Position);
        Position += 4;
        return result;
    }

    /// <summary>
    ///     Deserialize a <see cref="float" />
    /// </summary>
    public float GetFloat()
    {
        CheckAccess(Position + 3);
        var result = FastBitConverter.Read<float>(Buffer, Position);
        Position += 4;
        return result;
    }

    /// <summary>
    ///     Deserialize a <see cref="double" />
    /// </summary>
    public double GetDouble()
    {
        CheckAccess(Position + 7);
        var result = FastBitConverter.Read<double>(Buffer, Position);
        Position += 8;
        return result;
    }

    /// <summary>
    ///     Deserialize a <see cref="string" />
    /// </summary>
    public string GetString(int maxLength)
    {
        var bytesCount = GetInt();
        if (bytesCount <= 0 || bytesCount > maxLength * 2) return string.Empty;

        CheckAccess(Position + bytesCount - 1);
        var charCount = Encoding.UTF8.GetCharCount(Buffer, Position, bytesCount);
        if (charCount > maxLength) return string.Empty;

        var result = Encoding.UTF8.GetString(Buffer, Position, bytesCount);
        Position += bytesCount;
        return result;
    }

    /// <summary>
    ///     Deserialize a <see cref="string" />
    /// </summary>
    public string GetString()
    {
        var bytesCount = GetInt();
        if (bytesCount <= 0) return string.Empty;

        CheckAccess(Position + bytesCount - 1);
        var result = Encoding.UTF8.GetString(Buffer, Position, bytesCount);
        Position += bytesCount;
        return result;
    }

    /// <summary>
    ///     Deserialize a <see cref="Vector2" />
    /// </summary>
    public Vector2 GetVector2()
    {
        return new Vector2(GetFloat(), GetFloat());
    }

    /// <summary>
    ///     Deserialize a <see cref="Vector3" />
    /// </summary>
    public Vector3 GetVector3()
    {
        return new Vector3(GetFloat(), GetFloat(), GetFloat());
    }

    /// <summary>
    ///     Deserialize a <see cref="Vector4" />
    /// </summary>
    public Vector4 GetVector4()
    {
        return new Vector4(GetFloat(), GetFloat(), GetFloat(), GetFloat());
    }

    /// <summary>
    ///     Deserialize a <see cref="Quaternion" />
    /// </summary>
    public Quaternion GetQuaternion()
    {
        return new Quaternion(GetFloat(), GetFloat(), GetFloat(), GetFloat());
    }

    /// <summary>
    ///     Deserialize a <see cref="Matrix4x4" />
    /// </summary>
    public Matrix4x4 GetMatrix4X4()
    {
        return new Matrix4x4(
            GetFloat(), GetFloat(), GetFloat(), GetFloat(),
            GetFloat(), GetFloat(), GetFloat(), GetFloat(),
            GetFloat(), GetFloat(), GetFloat(), GetFloat(),
            GetFloat(), GetFloat(), GetFloat(), GetFloat());
    }

    /// <summary>
    ///     Deserialize a <see cref="bool" />
    /// </summary>
    /// <returns></returns>
    public bool GetBool()
    {
        return GetByte() > 0;
    }

    #endregion

    #region PeekMethods

    /// <summary>
    ///     Deserialize a <see cref="byte" /> without incrementing the position
    /// </summary>
    public byte PeekByte()
    {
        CheckAccess(Position);
        return Buffer[Position];
    }

    /// <summary>
    ///     Deserialize a <see cref="sbyte" /> without incrementing the position
    /// </summary>
    public sbyte PeekSByte()
    {
        CheckAccess(Position);

        return Unsafe.As<byte, sbyte>(ref Buffer[Position]);
    }

    /// <summary>
    ///     Deserialize a <see cref="bool" /> without incrementing the position
    /// </summary>
    public bool PeekBool()
    {
        CheckAccess(Position);

        return Buffer[Position] > 0;
    }

    /// <summary>
    ///     Deserialize a <see cref="ushort" /> without incrementing the position
    /// </summary>
    public ushort PeekUShort()
    {
        CheckAccess(Position);

        return FastBitConverter.Read<ushort>(Buffer, Position);
    }

    /// <summary>
    ///     Deserialize a <see cref="short" /> without incrementing the position
    /// </summary>
    public short PeekShort()
    {
        CheckAccess(Position);

        return FastBitConverter.Read<short>(Buffer, Position);
    }

    /// <summary>
    ///     Deserialize a <see cref="long" /> without incrementing the position
    /// </summary>
    public long PeekLong()
    {
        CheckAccess(Position);

        return FastBitConverter.Read<long>(Buffer, Position);
    }

    /// <summary>
    ///     Deserialize a <see cref="ulong" /> without incrementing the position
    /// </summary>
    public ulong PeekULong()
    {
        CheckAccess(Position);

        return FastBitConverter.Read<ulong>(Buffer, Position);
    }

    /// <summary>
    ///     Deserialize a <see cref="int" /> without incrementing the position
    /// </summary>
    public int PeekInt()
    {
        CheckAccess(Position);

        return FastBitConverter.Read<int>(Buffer, Position);
    }

    /// <summary>
    ///     Deserialize a <see cref="uint" /> without incrementing the position
    /// </summary>
    public uint PeekUInt()
    {
        CheckAccess(Position);

        return FastBitConverter.Read<uint>(Buffer, Position);
    }

    /// <summary>
    ///     Deserialize a <see cref="float" /> without incrementing the position
    /// </summary>
    public float PeekFloat()
    {
        CheckAccess(Position);

        return FastBitConverter.Read<float>(Buffer, Position);
    }

    /// <summary>
    ///     Deserialize a <see cref="double" /> without incrementing the position
    /// </summary>
    public double PeekDouble()
    {
        CheckAccess(Position);

        return FastBitConverter.Read<double>(Buffer, Position);
    }

    /// <summary>
    ///     Deserialize a <see cref="string" /> without incrementing the position
    /// </summary>
    public string PeekString(int maxLength)
    {
        CheckAccess(Position + 3);

        var bytesCount = FastBitConverter.Read<int>(Buffer, Position);
        if (bytesCount <= 0 || bytesCount > maxLength * 2) return string.Empty;

        CheckAccess(Position - 1 + bytesCount);

        var charCount = Encoding.UTF8.GetCharCount(Buffer, Position + 4, bytesCount);
        if (charCount > maxLength) return string.Empty;

        var result = Encoding.UTF8.GetString(Buffer, Position + 4, bytesCount);
        return result;
    }

    /// <summary>
    ///     Deserialize a <see cref="string" /> without incrementing the position
    /// </summary>
    public string PeekString()
    {
        CheckAccess(Position + 3);
        var bytesCount = FastBitConverter.Read<int>(Buffer, Position);
        if (bytesCount <= 0) return string.Empty;

        CheckAccess(Position - 1 + bytesCount);
        var result = Encoding.UTF8.GetString(Buffer, Position + 4, bytesCount);
        return result;
    }

    #endregion

    #region TryGetMethods

    /// <summary>
    ///     Try deserialize a <see cref="byte" />
    /// </summary>
    public bool TryGetByte(out byte result)
    {
        if (AvailableBytes >= 1)
        {
            result = GetByte();
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="sbyte" />
    /// </summary>
    public bool TryGetSByte(out sbyte result)
    {
        if (AvailableBytes >= 1)
        {
            result = GetSByte();
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="short" />
    /// </summary>
    public bool TryGetShort(out short result)
    {
        if (AvailableBytes >= 2)
        {
            result = GetShort();
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="ushort" />
    /// </summary>
    public bool TryGetUShort(out ushort result)
    {
        if (AvailableBytes >= 2)
        {
            result = GetUShort();
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="int" />
    /// </summary>
    public bool TryGetInt(out int result)
    {
        if (AvailableBytes >= 4)
        {
            result = GetInt();
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="uint" />
    /// </summary>
    public bool TryGetUInt(out uint result)
    {
        if (AvailableBytes >= 4)
        {
            result = GetUInt();
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="long" />
    /// </summary>
    public bool TryGetLong(out long result)
    {
        if (AvailableBytes >= 8)
        {
            result = GetLong();
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="ulong" />
    /// </summary>
    public bool TryGetULong(out ulong result)
    {
        if (AvailableBytes >= 8)
        {
            result = GetULong();
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="float" />
    /// </summary>
    public bool TryGetFloat(out float result)
    {
        if (AvailableBytes >= 4)
        {
            result = GetFloat();
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="double" />
    /// </summary>
    public bool TryGetDouble(out double result)
    {
        if (AvailableBytes >= 8)
        {
            result = GetDouble();
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="string" />
    /// </summary>
    public bool TryGetString(out string result)
    {
        if (AvailableBytes >= 4)
        {
            var bytesCount = PeekInt();
            if (AvailableBytes >= bytesCount + 4)
            {
                result = GetString();
                return true;
            }
        }

        result = string.Empty;
        return false;
    }

    /// <summary>
    ///     Try deserialize a <see cref="string" /> array
    /// </summary>
    public bool TryGetStringArray(out string[] result)
    {
        if (!TryGetUShort(out var size))
        {
            result = Array.Empty<string>();
            return false;
        }

        result = new string[size];
        for (var i = 0; i < size; i++)
            if (!TryGetString(out result[i]))
            {
                result = Array.Empty<string>();
                return false;
            }

        return true;
    }

    #endregion
}


/// <summary>
///     Serialize Data to a byte array
/// </summary>
public class DataWriter
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public DataWriter()
    {
        Buffer = new byte[64];
        Position = 0;
    }

    /// <summary>
    ///     Buffer of the writer
    /// </summary>
    public byte[] Buffer { get; private set; }

    /// <summary>
    ///     Current position
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    ///     Get the length of the writer
    /// </summary>
    public int Length => Position;

    /// <summary>
    ///     Offset the location of the writer
    /// </summary>
    /// <param name="offset"></param>
    public void AddOffset(int offset)
    {
        Position += offset;
    }

    /// <summary>
    ///     Reset the <see cref="DataWriter" />. This will only move the Location of the writer to 0
    /// </summary>
    public void Reset()
    {
        Position = 0;
    }

    /// <summary>
    ///     Check if the data at the given position is accessible
    /// </summary>
    /// <param name="pos"></param>
    public void CheckData(int pos)
    {
        ResizeIfNeed(pos);
    }

    /// <summary>
    ///     Resize if the position is out of bounds
    /// </summary>
    /// <param name="posCompare"></param>
    public void ResizeIfNeed(int posCompare)
    {
        var len = Buffer.Length;
        if (len > posCompare) return;
        while (len <= posCompare)
        {
            len += 1;
            len *= 2;
        }

        len = MathHelper.CeilPower2(len);
        var newBuffer = new byte[len];
        System.Buffer.BlockCopy(Buffer, 0, newBuffer, 0, Buffer.Length);

        Buffer = newBuffer;
    }

    /// <summary>
    ///     Serialize a <see cref="float" />
    /// </summary>
    public void Put(float value)
    {
        CheckData(Position + 4);
        FastBitConverter.Write(Buffer, Position, value);
        Position += 4;
    }

    /// <summary>
    ///     Serialize a <see cref="double" />
    /// </summary>
    public void Put(double value)
    {
        CheckData(Position + 8);
        FastBitConverter.Write(Buffer, Position, value);
        Position += 8;
    }

    /// <summary>
    ///     Serialize a <see cref="long" />
    /// </summary>
    public void Put(long value)
    {
        CheckData(Position + 8);
        FastBitConverter.Write(Buffer, Position, value);
        Position += 8;
    }

    /// <summary>
    ///     Serialize a <see cref="ulong" />
    /// </summary>
    public void Put(ulong value)
    {
        CheckData(Position + 8);
        FastBitConverter.Write(Buffer, Position, value);
        Position += 8;
    }

    /// <summary>
    ///     Serialize a <see cref="int" />
    /// </summary>
    public void Put(int value)
    {
        CheckData(Position + 4);
        FastBitConverter.Write(Buffer, Position, value);
        Position += 4;
    }

    /// <summary>
    ///     Serialize a <see cref="uint" />
    /// </summary>
    public void Put(uint value)
    {
        CheckData(Position + 4);
        FastBitConverter.Write(Buffer, Position, value);
        Position += 4;
    }

    /// <summary>
    ///     Serialize a <see cref="char" />
    /// </summary>
    public void Put(char value)
    {
        CheckData(Position + 2);
        FastBitConverter.Write(Buffer, Position, value);
        Position += 2;
    }

    /// <summary>
    ///     Serialize a <see cref="ushort" />
    /// </summary>
    public void Put(ushort value)
    {
        CheckData(Position + 2);
        FastBitConverter.Write(Buffer, Position, value);
        Position += 2;
    }

    /// <summary>
    ///     Serialize a <see cref="short" />
    /// </summary>
    public void Put(short value)
    {
        CheckData(Position + 2);
        FastBitConverter.Write(Buffer, Position, value);
        Position += 2;
    }

    /// <summary>
    ///     Serialize a <see cref="sbyte" />
    /// </summary>
    public void Put(sbyte value)
    {
        CheckData(Position + 1);
        Unsafe.As<byte, sbyte>(ref Buffer[Position]) = value;
        Position++;
    }

    /// <summary>
    ///     Serialize a <see cref="byte" />
    /// </summary>
    public void Put(byte value)
    {
        CheckData(Position + 1);
        Buffer[Position] = value;
        Position++;
    }

    /// <summary>
    ///     Serialize a <see cref="IPEndPoint" />
    /// </summary>
    public void Put(IPEndPoint endPoint)
    {
        Put(endPoint.Address.ToString());
        Put(endPoint.Port);
    }

    /// <summary>
    ///     Serialize a <see cref="string" />
    /// </summary>
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
        Encoding.UTF8.GetBytes(value.AsSpan(), Buffer.AsSpan(Position));

        Position += bytesCount;
    }

    /// <summary>
    ///     Serialize a <see cref="Vector2" />
    /// </summary>
    public void Put(Vector2 value)
    {
        Put(value.X);
        Put(value.Y);
    }

    /// <summary>
    ///     Serialize a <see cref="Vector3" />
    /// </summary>
    public void Put(Vector3 value)
    {
        Put(value.X);
        Put(value.Y);
        Put(value.Z);
    }

    /// <summary>
    ///     Serialize a <see cref="Vector4" />
    /// </summary>
    public void Put(Vector4 value)
    {
        Put(value.X);
        Put(value.Y);
        Put(value.Z);
        Put(value.W);
    }

    /// <summary>
    ///     Serialize a <see cref="Quaternion" />
    /// </summary>
    public void Put(Quaternion value)
    {
        Put(value.X);
        Put(value.Y);
        Put(value.Z);
        Put(value.W);
    }

    /// <summary>
    ///     Serialize a <see cref="Matrix4x4" />
    /// </summary>
    public void Put(Matrix4x4 value)
    {
        Put(value.M11);
        Put(value.M12);
        Put(value.M13);
        Put(value.M14);

        Put(value.M21);
        Put(value.M22);
        Put(value.M23);
        Put(value.M24);

        Put(value.M31);
        Put(value.M32);
        Put(value.M33);
        Put(value.M34);

        Put(value.M41);
        Put(value.M42);
        Put(value.M43);
        Put(value.M44);
    }

    /// <summary>
    ///     Serialize a <see cref="bool" />
    /// </summary>
    public void Put(bool value)
    {
        if (value)
            Put((byte)1);
        else
            Put((byte)0);
    }

    /// <summary>
    /// Add a reference to the current position in the writer to write to later
    /// </summary>
    /// <typeparam name="T">Type to write later, must be numeric</typeparam>
    /// <exception cref="MintyCoreException">If T is not numeric</exception>
    /// <returns>A "reference" to write later on</returns>
    public unsafe ValueRef<T> AddValueRef<T>() where T : unmanaged
    {
        Logger.AssertAndThrow(IsNumeric(), "Value refs are only valid for numeric types", "Utils");
        
        CheckData(Position + sizeof(T));
        var reference = new ValueRef<T>(this, Position);
        
        //Zero the data
        Unsafe.As<byte, T>(ref Buffer[Position]) = default;
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
    /// Stores a "reference" to a variable inside a <see cref="DataWriter"/>
    /// </summary>
    /// <typeparam name="T">Type of the variable to reference</typeparam>
    public readonly struct ValueRef<T> where T : unmanaged
    {
        private readonly DataWriter _parent;
        private readonly int _dataPosition;

        internal ValueRef(DataWriter parent, int dataPosition)
        {
            _parent = parent;
            _dataPosition = dataPosition;
        }

        /// <summary>
        /// Set the value of the referenced variable
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(T value)
        {
            FastBitConverter.Write(_parent.Buffer, _dataPosition, value);
        }
    }
}

internal static class FastBitConverter
{

    public static unsafe void Write<T>(byte[] bytes, int startIndex, T value) where T : unmanaged
    {
        Unsafe.As<byte, T>(ref bytes[startIndex]) = value;

        if (BitConverter.IsLittleEndian) return;
        bytes.AsSpan(startIndex, sizeof(T)).Reverse();
    }

    public static unsafe T Read<T>(byte[] bytes, int index) where T : unmanaged
    {
        if (!BitConverter.IsLittleEndian)
            //If this machine is using big endian we need to convert the data
            bytes.AsSpan(index, sizeof(T)).Reverse();

        return Unsafe.As<byte, T>(ref bytes[index]);
    }
}