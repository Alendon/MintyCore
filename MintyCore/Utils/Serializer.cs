using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MintyCore.Utils.Maths;

namespace MintyCore.Utils
{
    /// <summary>
    ///     DataReader class used to deserialize data from byte arrays
    /// </summary>
    public class DataReader
    {
        public byte[] Buffer { get; }
        public int DataSize => Buffer.Length;
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
            var result = FastBitConverter.ReadUShort(Buffer, Position);
            Position += 2;
            return result;
        }

        /// <summary>
        ///     Deserialize a <see cref="short" />
        /// </summary>
        public short GetShort()
        {
            CheckAccess(Position + 1);
            var result = FastBitConverter.ReadShort(Buffer, Position);
            Position += 2;
            return result;
        }

        /// <summary>
        ///     Deserialize a <see cref="long" />
        /// </summary>
        public long GetLong()
        {
            CheckAccess(Position + 7);
            var result = FastBitConverter.ReadLong(Buffer, Position);
            Position += 8;
            return result;
        }

        /// <summary>
        ///     Deserialize a <see cref="ulong" />
        /// </summary>
        public ulong GetULong()
        {
            CheckAccess(Position + 7);
            var result = FastBitConverter.ReadULong(Buffer, Position);
            Position += 8;
            return result;
        }

        /// <summary>
        ///     Deserialize a <see cref="int" />
        /// </summary>
        public int GetInt()
        {
            CheckAccess(Position + 3);
            var result = FastBitConverter.ReadInt(Buffer, Position);
            Position += 4;
            return result;
        }

        /// <summary>
        ///     Deserialize a <see cref="uint" />
        /// </summary>
        public uint GetUInt()
        {
            CheckAccess(Position + 3);
            var result = FastBitConverter.ReadUInt(Buffer, Position);
            Position += 4;
            return result;
        }

        /// <summary>
        ///     Deserialize a <see cref="float" />
        /// </summary>
        public float GetFloat()
        {
            CheckAccess(Position + 3);
            var result = FastBitConverter.ReadFloat(Buffer, Position);
            Position += 4;
            return result;
        }

        /// <summary>
        ///     Deserialize a <see cref="double" />
        /// </summary>
        public double GetDouble()
        {
            CheckAccess(Position + 7);
            var result = FastBitConverter.ReadDouble(Buffer, Position);
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

        public bool GetBool()
        {
            CheckAccess(Position);
            return Buffer[Position] > 0;
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

            return FastBitConverter.ReadUShort(Buffer, Position);
        }

        /// <summary>
        ///     Deserialize a <see cref="short" /> without incrementing the position
        /// </summary>
        public short PeekShort()
        {
            CheckAccess(Position);

            return FastBitConverter.ReadShort(Buffer, Position);
        }

        /// <summary>
        ///     Deserialize a <see cref="long" /> without incrementing the position
        /// </summary>
        public long PeekLong()
        {
            CheckAccess(Position);

            return FastBitConverter.ReadLong(Buffer, Position);
        }

        /// <summary>
        ///     Deserialize a <see cref="ulong" /> without incrementing the position
        /// </summary>
        public ulong PeekULong()
        {
            CheckAccess(Position);

            return FastBitConverter.ReadULong(Buffer, Position);
        }

        /// <summary>
        ///     Deserialize a <see cref="int" /> without incrementing the position
        /// </summary>
        public int PeekInt()
        {
            CheckAccess(Position);

            return FastBitConverter.ReadInt(Buffer, Position);
        }

        /// <summary>
        ///     Deserialize a <see cref="uint" /> without incrementing the position
        /// </summary>
        public uint PeekUInt()
        {
            CheckAccess(Position);

            return FastBitConverter.ReadUInt(Buffer, Position);
        }

        /// <summary>
        ///     Deserialize a <see cref="float" /> without incrementing the position
        /// </summary>
        public float PeekFloat()
        {
            CheckAccess(Position);

            return FastBitConverter.ReadFloat(Buffer, Position);
        }

        /// <summary>
        ///     Deserialize a <see cref="double" /> without incrementing the position
        /// </summary>
        public double PeekDouble()
        {
            CheckAccess(Position);

            return FastBitConverter.ReadDouble(Buffer, Position);
        }

        /// <summary>
        ///     Deserialize a <see cref="string" /> without incrementing the position
        /// </summary>
        public string PeekString(int maxLength)
        {
            CheckAccess(Position + 3);

            var bytesCount = FastBitConverter.ReadInt(Buffer, Position);
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
            var bytesCount = FastBitConverter.ReadInt(Buffer, Position);
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
        public byte[] Buffer { get; private set; }
        public int Position { get; private set; }

        public DataWriter()
        {
            Buffer = new byte[64];
            Position = 0;
        }

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
            byte[] newBuffer = new byte[len];
            System.Buffer.BlockCopy(Buffer, 0, newBuffer, 0, Buffer.Length);

            Buffer = newBuffer;
        }

        /// <summary>
        ///     Get the length of the writer
        /// </summary>
        public int Length => Position;

        /// <summary>
        ///     Serialize a <see cref="float" />
        /// </summary>
        public void Put(float value)
        {
            CheckData(Position + 4);
            FastBitConverter.WriteBytes(Buffer, Position, value);
            Position += 4;
        }

        /// <summary>
        ///     Serialize a <see cref="double" />
        /// </summary>
        public void Put(double value)
        {
            CheckData(Position + 8);
            FastBitConverter.WriteBytes(Buffer, Position, value);
            Position += 8;
        }

        /// <summary>
        ///     Serialize a <see cref="long" />
        /// </summary>
        public void Put(long value)
        {
            CheckData(Position + 8);
            FastBitConverter.WriteBytes(Buffer, Position, value);
            Position += 8;
        }

        /// <summary>
        ///     Serialize a <see cref="ulong" />
        /// </summary>
        public void Put(ulong value)
        {
            CheckData(Position + 8);
            FastBitConverter.WriteBytes(Buffer, Position, value);
            Position += 8;
        }

        /// <summary>
        ///     Serialize a <see cref="int" />
        /// </summary>
        public void Put(int value)
        {
            CheckData(Position + 4);
            FastBitConverter.WriteBytes(Buffer, Position, value);
            Position += 4;
        }

        /// <summary>
        ///     Serialize a <see cref="uint" />
        /// </summary>
        public void Put(uint value)
        {
            CheckData(Position + 4);
            FastBitConverter.WriteBytes(Buffer, Position, value);
            Position += 4;
        }

        /// <summary>
        ///     Serialize a <see cref="char" />
        /// </summary>
        public void Put(char value)
        {
            CheckData(Position + 2);
            FastBitConverter.WriteBytes(Buffer, Position, value);
            Position += 2;
        }

        /// <summary>
        ///     Serialize a <see cref="ushort" />
        /// </summary>
        public void Put(ushort value)
        {
            CheckData(Position + 2);
            FastBitConverter.WriteBytes(Buffer, Position, value);
            Position += 2;
        }

        /// <summary>
        ///     Serialize a <see cref="short" />
        /// </summary>
        public void Put(short value)
        {
            CheckData(Position + 2);
            FastBitConverter.WriteBytes(Buffer, Position, value);
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

        public void Put(bool value)
        {
            if (value) 
                Put((byte)1);
            else 
                Put((byte)0);
        }
    }

    internal static class FastBitConverter
    {
        public static void WriteBytes(byte[] bytes, int startIndex, double value)
        {
            Unsafe.As<byte, double>(ref bytes[startIndex]) = value;

            if (BitConverter.IsLittleEndian) return;
            //If this is machine is using big endian, convert the data to little endian
            bytes.AsSpan(startIndex, sizeof(double)).Reverse();
        }

        public static double ReadDouble(byte[] bytes, int index)
        {
            if (!BitConverter.IsLittleEndian)
            {
                //If this machine is using big endian we need to convert the data
                bytes.AsSpan(index, sizeof(double)).Reverse();
            }

            return Unsafe.As<byte, double>(ref bytes[index]);
        }

        public static void WriteBytes(byte[] bytes, int startIndex, float value)
        {
            Unsafe.As<byte, float>(ref bytes[startIndex]) = value;

            if (BitConverter.IsLittleEndian) return;
            //If this is machine is using big endian, convert the data to little endian
            bytes.AsSpan(startIndex, sizeof(float)).Reverse();
        }

        public static float ReadFloat(byte[] bytes, int index)
        {
            if (!BitConverter.IsLittleEndian)
            {
                //If this machine is using big endian we need to convert the data
                bytes.AsSpan(index, sizeof(float)).Reverse();
            }

            return Unsafe.As<byte, float>(ref bytes[index]);
        }

        public static void WriteBytes(byte[] bytes, int startIndex, short value)
        {
            Unsafe.As<byte, short>(ref bytes[startIndex]) = value;

            if (BitConverter.IsLittleEndian) return;
            //If this is machine is using big endian, convert the data to little endian
            bytes.AsSpan(startIndex, sizeof(short)).Reverse();
        }

        public static short ReadShort(byte[] bytes, int index)
        {
            if (!BitConverter.IsLittleEndian)
            {
                //If this machine is using big endian we need to convert the data
                bytes.AsSpan(index, sizeof(short)).Reverse();
            }

            return Unsafe.As<byte, short>(ref bytes[index]);
        }

        public static void WriteBytes(byte[] bytes, int startIndex, ushort value)
        {
            Unsafe.As<byte, ushort>(ref bytes[startIndex]) = value;

            if (BitConverter.IsLittleEndian) return;
            //If this is machine is using big endian, convert the data to little endian
            bytes.AsSpan(startIndex, sizeof(ushort)).Reverse();
        }

        public static ushort ReadUShort(byte[] bytes, int index)
        {
            if (!BitConverter.IsLittleEndian)
            {
                //If this machine is using big endian we need to convert the data
                bytes.AsSpan(index, sizeof(ushort)).Reverse();
            }

            return Unsafe.As<byte, ushort>(ref bytes[index]);
        }

        public static void WriteBytes(byte[] bytes, int startIndex, int value)
        {
            Unsafe.As<byte, int>(ref bytes[startIndex]) = value;

            if (BitConverter.IsLittleEndian) return;
            //If this is machine is using big endian, convert the data to little endian
            bytes.AsSpan(startIndex, sizeof(int)).Reverse();
        }

        public static int ReadInt(byte[] bytes, int index)
        {
            if (!BitConverter.IsLittleEndian)
            {
                //If this machine is using big endian we need to convert the data
                bytes.AsSpan(index, sizeof(int)).Reverse();
            }

            return Unsafe.As<byte, int>(ref bytes[index]);
        }

        public static void WriteBytes(byte[] bytes, int startIndex, uint value)
        {
            Unsafe.As<byte, uint>(ref bytes[startIndex]) = value;

            if (BitConverter.IsLittleEndian) return;
            //If this is machine is using big endian, convert the data to little endian
            bytes.AsSpan(startIndex, sizeof(uint)).Reverse();
        }

        public static uint ReadUInt(byte[] bytes, int index)
        {
            if (!BitConverter.IsLittleEndian)
            {
                //If this machine is using big endian we need to convert the data
                bytes.AsSpan(index, sizeof(uint)).Reverse();
            }

            return Unsafe.As<byte, uint>(ref bytes[index]);
        }

        public static void WriteBytes(byte[] bytes, int startIndex, long value)
        {
            Unsafe.As<byte, long>(ref bytes[startIndex]) = value;

            if (BitConverter.IsLittleEndian) return;
            //If this is machine is using big endian, convert the data to little endian
            bytes.AsSpan(startIndex, sizeof(long)).Reverse();
        }

        public static long ReadLong(byte[] bytes, int index)
        {
            if (!BitConverter.IsLittleEndian)
            {
                //If this machine is using big endian we need to convert the data
                bytes.AsSpan(index, sizeof(long)).Reverse();
            }

            return Unsafe.As<byte, long>(ref bytes[index]);
        }

        public static void WriteBytes(byte[] bytes, int startIndex, ulong value)
        {
            Unsafe.As<byte, ulong>(ref bytes[startIndex]) = value;

            if (BitConverter.IsLittleEndian) return;
            //If this is machine is using big endian, convert the data to little endian
            bytes.AsSpan(startIndex, sizeof(ulong)).Reverse();
        }

        public static ulong ReadULong(byte[] bytes, int index)
        {
            if (!BitConverter.IsLittleEndian)
            {
                //If this machine is using big endian we need to convert the data
                bytes.AsSpan(index, sizeof(ulong)).Reverse();
            }

            return Unsafe.As<byte, ulong>(ref bytes[index]);
        }
    }
}