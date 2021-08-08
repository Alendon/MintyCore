using System;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace MintyCore.Utils
{
	internal unsafe struct UnmanagedDataReader : IDisposable
	{
		private byte* _buffer;
		internal int Position;
		internal int DataSize;
		private byte _isInitializedByte;

		private byte _disposeQueued;

		internal bool DisposeQueued
		{
			get => _disposeQueued != 0;
			set => _disposeQueued = value ? /* We need an conversion for Unity*/ ( byte )1 : ( byte )0;
		}

		public bool IsInitialized
		{
			get => _isInitializedByte != 0;
			private set => _isInitializedByte = value ? ( byte )1 : ( byte )0;
		}

		public void InitializeWithCopy( byte* data, int length, int startPosition = 0 )
		{
#if DEBUG
			if ( IsInitialized )
				throw new AccessViolationException(
					"Tried to initialize UnmanagedNetDataReader but its already initialized" );
#endif

			_buffer = ( byte* )AllocationHandler.Malloc( length );
			System.Buffer.MemoryCopy( data, _buffer, length, length );

			Position = startPosition;
			DataSize = length;

			IsInitialized = true;
		}

		public void InitializeWithoutCopy( byte* data, int length, int startPosition = 0 )
		{
#if DEBUG
			if ( IsInitialized )
				throw new AccessViolationException(
					"Tried to initialize UnmanagedNetDataReader but its already initialized" );
#endif


			_buffer = data;

			Position = startPosition;
			DataSize = length;

			IsInitialized = true;
		}

		public byte* GetCurrentPointer( int offset ) => _buffer + Position + offset;

		public byte* GetCurrentPointer() => _buffer + Position;

		public byte* GetPointer() => _buffer;

		internal void CheckAndThrow( int pos )
		{
			if ( pos >= DataSize )
			{
				throw new IndexOutOfRangeException();
			}
		}

		public void Dispose()
		{
			AllocationHandler.Free( new IntPtr( _buffer ) );
		}
	}

	/// <summary>
	/// DataReader class used to deserialize data from byte arrays
	/// </summary>
	[DebuggerTypeProxy( typeof( DebuggerProxy ) )]
	public unsafe struct DataReader : IDisposable
	{
		private readonly UnmanagedDataReader* _internalReader;

		/// <summary>
		/// Get the size of the raw data
		/// </summary>
		public int RawDataSize
		{
			get
			{
				return _internalReader->DataSize;
			}
		}

		/// <summary>
		/// Check if the <see cref="DataReader"/> is null
		/// </summary>
		public bool IsNull
		{
			get
			{
				return !_internalReader->IsInitialized || _internalReader->DataSize <= 0;
			}
		}

		/// <summary>
		/// Get/Set the current position of the reader
		/// </summary>
		public int Position
		{
			internal set
			{
				_internalReader->Position = value;
			}

			get
			{
				return _internalReader->Position;
			}
		}

		/// <summary>
		/// Check if the end of the data is reached
		/// </summary>
		public bool EndOfData
		{
			get
			{
				return _internalReader->Position >= _internalReader->DataSize;
			}
		}

		/// <summary>
		/// Get the available bytes left
		/// </summary>
		public int AvailableBytes
		{
			get
			{
				return _internalReader->DataSize - _internalReader->Position;
			}
		}

		/// <summary>
		/// Get a byte ptr to the current position
		/// </summary>
		/// <returns></returns>
		private byte* GetBytePointer()
		{
			return _internalReader->GetCurrentPointer();
		}

		/// <summary>
		/// Get a byte ptr to the origin of the reader
		/// </summary>
		/// <returns></returns>
		private byte* GetOriginPointer()
		{
			return _internalReader->GetPointer();
		}

		/// <summary>
		/// get a byte ptr with a given offset from the current position
		/// </summary>
		/// <param name="offset"></param>
		/// <returns></returns>
		private byte* GetBytePointer( int offset )
		{
			return _internalReader->GetCurrentPointer( offset );
		}

		/// <summary>
		/// Create a new <see cref="DataReader"/>
		/// </summary>
		public DataReader( byte[] source )
		{
			_internalReader = ( UnmanagedDataReader* )AllocationHandler.Malloc<UnmanagedDataReader>();
			*_internalReader = default;

			fixed ( byte* point = source )
			{
				_internalReader->InitializeWithCopy( point, source.Length );
			}
		}

		/// <summary>
		/// Create a new <see cref="DataReader"/>
		/// </summary>
		public DataReader( byte[] source, int position )
		{
			_internalReader = ( UnmanagedDataReader* )AllocationHandler.Malloc<UnmanagedDataReader>();
			*_internalReader = default;

			fixed ( byte* point = source )
			{
				_internalReader->InitializeWithCopy( point, source.Length, position );
			}
		}

		/// <summary>
		/// Create a new <see cref="DataReader"/>
		/// </summary>
		public DataReader( byte* data, int length, int position = 0 )
		{
			_internalReader = ( UnmanagedDataReader* )AllocationHandler.Malloc<UnmanagedDataReader>();
			*_internalReader = default;

			_internalReader->InitializeWithoutCopy( data, length, position );

		}

		/// <summary>
		/// Check if the access at <paramref name="position"/> is valid
		/// </summary>
		/// <param name="position"></param>
		public void CheckAccess( int position )
		{
#if DEBUG
			if ( !AllocationHandler.AllocationValid( ( IntPtr )_internalReader ) )
			{
				throw new Exception( "Internal reader is not valid" );
			}
#endif
			_internalReader->CheckAndThrow( position );
		}

		#region GetMethods

		/// <summary>
		/// Deserialize a <see cref="byte"/>
		/// </summary>
		public byte GetByte()
		{
			CheckAccess( Position );
			byte res = *GetBytePointer();
			Position += 1;
			return res;
		}

		/// <summary>
		/// Deserialize a <see cref="sbyte"/>
		/// </summary>
		public sbyte GetSByte()
		{
			CheckAccess( Position );
			var res = ( sbyte )*GetBytePointer();
			Position += 1;
			return res;
		}

		/// <summary>
		/// Deserialize a <see cref="char"/>
		/// </summary>
		public char GetChar()
		{
			CheckAccess( Position + 1 );
			char result = FastBitConverter.ReadChar( GetBytePointer() );
			Position += 2;
			return result;
		}

		/// <summary>
		/// Deserialize a <see cref="ushort"/>
		/// </summary>
		public ushort GetUShort()
		{
			CheckAccess( Position + 1 );
			ushort result = FastBitConverter.ReadUShort( GetBytePointer() );
			Position += 2;
			return result;
		}

		/// <summary>
		/// Deserialize a <see cref="short"/>
		/// </summary>
		public short GetShort()
		{
			CheckAccess( Position + 1 );
			short result = FastBitConverter.ReadShort( GetBytePointer() );
			Position += 2;
			return result;
		}

		/// <summary>
		/// Deserialize a <see cref="long"/>
		/// </summary>
		public long GetLong()
		{
			CheckAccess( Position + 7 );
			long result = FastBitConverter.ReadLong( GetBytePointer() );
			Position += 8;
			return result;
		}

		/// <summary>
		/// Deserialize a <see cref="ulong"/>
		/// </summary>
		public ulong GetULong()
		{
			CheckAccess( Position + 7 );
			ulong result = FastBitConverter.ReadULong( GetBytePointer() );
			Position += 8;
			return result;
		}

		/// <summary>
		/// Deserialize a <see cref="int"/>
		/// </summary>
		public int GetInt()
		{
			CheckAccess( Position + 3 );
			int result = FastBitConverter.ReadInt( GetBytePointer() );
			Position += 4;
			return result;
		}

		/// <summary>
		/// Deserialize a <see cref="uint"/>
		/// </summary>
		public uint GetUInt()
		{
			CheckAccess( Position + 3 );
			uint result = FastBitConverter.ReadUInt( GetBytePointer() );
			Position += 4;
			return result;
		}

		/// <summary>
		/// Deserialize a <see cref="float"/>
		/// </summary>
		public float GetFloat()
		{
			CheckAccess( Position + 3 );
			float result = FastBitConverter.ReadFloat( GetBytePointer() );
			Position += 4;
			return result;
		}

		/// <summary>
		/// Deserialize a <see cref="double"/>
		/// </summary>
		public double GetDouble()
		{
			CheckAccess( Position + 7 );
			double result = FastBitConverter.ReadDouble( GetBytePointer() );
			Position += 8;
			return result;
		}

		/// <summary>
		/// Deserialize a <see cref="string"/>
		/// </summary>
		public string GetString( int maxLength )
		{
			int bytesCount = GetInt();
			if ( bytesCount <= 0 || bytesCount > maxLength * 2 )
			{
				return string.Empty;
			}

			CheckAccess( Position + bytesCount - 1 );
			int charCount = Encoding.UTF8.GetCharCount( GetBytePointer(), bytesCount );
			if ( charCount > maxLength )
			{
				return string.Empty;
			}

			string result = Encoding.UTF8.GetString( GetBytePointer(), bytesCount );
			Position += bytesCount;
			return result;
		}

		/// <summary>
		/// Deserialize a <see cref="string"/>
		/// </summary>
		public string GetString()
		{
			int bytesCount = GetInt();
			if ( bytesCount <= 0 )
			{
				return string.Empty;
			}

			CheckAccess( Position + bytesCount - 1 );
			string result = Encoding.UTF8.GetString( GetBytePointer(), bytesCount );
			Position += bytesCount;
			return result;
		}

		/// <summary>
		/// Get the remaining data as new <see cref="DataReader"/>
		/// </summary>
		public DataReader GetBytesAsReader()
		{
			int byteCount = GetInt();
			CheckAccess( Position + byteCount - 1 );
			var pointer = GetBytePointer();
			return new DataReader( pointer, byteCount );
		}

		/// <summary>
		/// Get a pointer to the current position and remaining byte count
		/// </summary>
		public (IntPtr data, int length) GetBytesWithoutCopy()
		{
			int byteCount = GetInt();
			CheckAccess( Position + byteCount - 1 );

			byte* data = GetBytePointer();
			Position += byteCount;

			return (new IntPtr( data ), byteCount);
		}

		/// <summary>
		/// Deserialize a <see cref="Vector2"/>
		/// </summary>
		public Vector2 GetVector2()
		{
			return new Vector2(GetFloat(), GetFloat());
		}

		/// <summary>
		/// Deserialize a <see cref="Vector3"/>
		/// </summary>
		public Vector3 GetVector3()
		{
			return new Vector3(GetFloat(), GetFloat(), GetFloat());
		}

		/// <summary>
		/// Deserialize a <see cref="Vector4"/>
		/// </summary>
		public Vector4 GetVector4()
		{
			return new Vector4(GetFloat(), GetFloat(), GetFloat(), GetFloat());
		}

		/// <summary>
		/// Deserialize a <see cref="Quaternion"/>
		/// </summary>
		public Quaternion GetQuaternion()
		{
			return new Quaternion(GetFloat(), GetFloat(), GetFloat(), GetFloat());
		}

		/// <summary>
		/// Deserialize a <see cref="Matrix4x4"/>
		/// </summary>
		public Matrix4x4 GetMatrix4x4()
		{
			return new Matrix4x4(
				GetFloat(), GetFloat(), GetFloat(), GetFloat(), 
				GetFloat(), GetFloat(), GetFloat(), GetFloat(), 
				GetFloat(), GetFloat(), GetFloat(), GetFloat(), 
				GetFloat(), GetFloat(), GetFloat(), GetFloat());
		}

		#endregion

		#region PeekMethods

		/// <summary>
		/// Deserialize a <see cref="byte"/> without incrementing the position
		/// </summary>
		public byte PeekByte()
		{
			CheckAccess( Position );
			return *GetBytePointer();
		}

		/// <summary>
		/// Deserialize a <see cref="sbyte"/> without incrementing the position
		/// </summary>
		public sbyte PeekSByte()
		{
			CheckAccess( Position );

			return ( sbyte )*GetBytePointer();
		}

		/// <summary>
		/// Deserialize a <see cref="bool"/> without incrementing the position
		/// </summary>
		public bool PeekBool()
		{
			CheckAccess( Position );

			return *GetBytePointer() > 0;
		}

		/// <summary>
		/// Deserialize a <see cref="char"/> without incrementing the position
		/// </summary>
		public char PeekChar()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadChar( GetBytePointer() );
		}

		/// <summary>
		/// Deserialize a <see cref="ushort"/> without incrementing the position
		/// </summary>
		public ushort PeekUShort()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadUShort( GetBytePointer() );
		}

		/// <summary>
		/// Deserialize a <see cref="short"/> without incrementing the position
		/// </summary>
		public short PeekShort()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadShort( GetBytePointer() );
		}

		/// <summary>
		/// Deserialize a <see cref="long"/> without incrementing the position
		/// </summary>
		public long PeekLong()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadLong( GetBytePointer() );
		}

		/// <summary>
		/// Deserialize a <see cref="ulong"/> without incrementing the position
		/// </summary>
		public ulong PeekULong()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadULong( GetBytePointer() );
		}

		/// <summary>
		/// Deserialize a <see cref="int"/> without incrementing the position
		/// </summary>
		public int PeekInt()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadInt( GetBytePointer() );
		}

		/// <summary>
		/// Deserialize a <see cref="uint"/> without incrementing the position
		/// </summary>
		public uint PeekUInt()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadUInt( GetBytePointer() );
		}

		/// <summary>
		/// Deserialize a <see cref="float"/> without incrementing the position
		/// </summary>
		public float PeekFloat()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadFloat( GetBytePointer() );
		}

		/// <summary>
		/// Deserialize a <see cref="double"/> without incrementing the position
		/// </summary>
		public double PeekDouble()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadDouble( GetBytePointer() );
		}

		/// <summary>
		/// Deserialize a <see cref="string"/> without incrementing the position
		/// </summary>
		public string PeekString( int maxLength )
		{
			CheckAccess( Position + 3 );

			int bytesCount = FastBitConverter.ReadInt( GetBytePointer() );
			if ( bytesCount <= 0 || bytesCount > maxLength * 2 )
			{
				return string.Empty;
			}

			CheckAccess( Position - 1 + bytesCount );

			int charCount = Encoding.UTF8.GetCharCount( GetBytePointer( 4 ), bytesCount );
			if ( charCount > maxLength )
			{
				return string.Empty;
			}

			string result = Encoding.UTF8.GetString( GetBytePointer( 4 ), bytesCount );
			return result;
		}

		/// <summary>
		/// Deserialize a <see cref="string"/> without incrementing the position
		/// </summary>
		public string PeekString()
		{
			CheckAccess( Position + 3 );
			int bytesCount = FastBitConverter.ReadInt( GetBytePointer() );
			if ( bytesCount <= 0 )
			{
				return string.Empty;
			}

			CheckAccess( Position - 1 + bytesCount );
			string result = Encoding.UTF8.GetString( GetBytePointer( 4 ), bytesCount );
			return result;
		}

		#endregion

		#region TryGetMethods

		/// <summary>
		/// Try deserialize a <see cref="byte"/> 
		/// </summary>
		public bool TryGetByte( out byte result )
		{
			if ( AvailableBytes >= 1 )
			{
				result = GetByte();
				return true;
			}

			result = 0;
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="sbyte"/> 
		/// </summary>
		public bool TryGetSByte( out sbyte result )
		{
			if ( AvailableBytes >= 1 )
			{
				result = GetSByte();
				return true;
			}

			result = 0;
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="char"/> 
		/// </summary>
		public bool TryGetChar( out char result )
		{
			if ( AvailableBytes >= 2 )
			{
				result = GetChar();
				return true;
			}

			result = '\0';
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="short"/> 
		/// </summary>
		public bool TryGetShort( out short result )
		{
			if ( AvailableBytes >= 2 )
			{
				result = GetShort();
				return true;
			}

			result = 0;
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="ushort"/> 
		/// </summary>
		public bool TryGetUShort( out ushort result )
		{
			if ( AvailableBytes >= 2 )
			{
				result = GetUShort();
				return true;
			}

			result = 0;
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="int"/> 
		/// </summary>
		public bool TryGetInt( out int result )
		{
			if ( AvailableBytes >= 4 )
			{
				result = GetInt();
				return true;
			}

			result = 0;
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="uint"/> 
		/// </summary>
		public bool TryGetUInt( out uint result )
		{
			if ( AvailableBytes >= 4 )
			{
				result = GetUInt();
				return true;
			}

			result = 0;
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="long"/> 
		/// </summary>
		public bool TryGetLong( out long result )
		{
			if ( AvailableBytes >= 8 )
			{
				result = GetLong();
				return true;
			}

			result = 0;
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="ulong"/> 
		/// </summary>
		public bool TryGetULong( out ulong result )
		{
			if ( AvailableBytes >= 8 )
			{
				result = GetULong();
				return true;
			}

			result = 0;
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="float"/> 
		/// </summary>
		public bool TryGetFloat( out float result )
		{
			if ( AvailableBytes >= 4 )
			{
				result = GetFloat();
				return true;
			}

			result = 0;
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="double"/> 
		/// </summary>
		public bool TryGetDouble( out double result )
		{
			if ( AvailableBytes >= 8 )
			{
				result = GetDouble();
				return true;
			}

			result = 0;
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="string"/> 
		/// </summary>
		public bool TryGetString( out string result )
		{
			if ( AvailableBytes >= 4 )
			{
				var bytesCount = PeekInt();
				if ( AvailableBytes >= bytesCount + 4 )
				{
					result = GetString();
					return true;
				}
			}

			result = null;
			return false;
		}

		/// <summary>
		/// Try deserialize a <see cref="string"/> array 
		/// </summary>
		public bool TryGetStringArray( out string[] result )
		{
			if ( !TryGetUShort( out ushort size ) )
			{
				result = null;
				return false;
			}

			result = new string[size];
			for ( int i = 0; i < size; i++ )
			{
				if ( !TryGetString( out result[i] ) )
				{
					result = null;
					return false;
				}
			}

			return true;
		}

		#endregion


		/// <inheritdoc/>
		public void Dispose()
		{

			if ( _internalReader->DisposeQueued ) return;
			_internalReader->DisposeQueued = true;

			_internalReader->Dispose();
			AllocationHandler.Free( ( IntPtr )_internalReader );
		}

		/// <summary>
		/// Dispose the data reader but not the original data
		/// </summary>
		public void DisposeKeepData()
		{
			if ( _internalReader->DisposeQueued ) return;
			_internalReader->DisposeQueued = true;

			AllocationHandler.Free( ( IntPtr )_internalReader );
		}

		private struct DebuggerProxy
		{
			private DataReader reader;
			public DebuggerProxy( DataReader writer )
			{
				this.reader = writer;
			}

			public byte[] Items
			{
				get
				{
					byte[] result = new byte[reader.RawDataSize];
					Marshal.Copy( ( IntPtr )reader.GetOriginPointer(), result, 0, result.Length );
					return result;
				}
			}
		}
	}

	internal unsafe struct UnmanagedDataWriter : IDisposable
	{
		private byte* _data;
		internal int Position;
		private const int InitialSize = 64;
		internal int Capacity;

		private byte _initializedByte;
		private byte _disposeQueued;
		private byte _disposeLocked;

		internal bool DisposeQueued
		{
			get => _disposeQueued != 0;
			set => _disposeQueued = value ? /*We need an conversion for Unity*/ ( byte )1 : ( byte )0;
		}

		internal bool DisposeLocked
		{
			get => _disposeLocked != 0;
			set => _disposeLocked = value ? ( byte )1 : ( byte )0;
		}

		internal bool Initialized
		{
			get => _initializedByte == 1;
			private set => _initializedByte = value ? ( byte )1 : ( byte )0;
		}

		internal void Setup()
		{
#if DEBUG
			if ( Initialized )
				throw new AccessViolationException(
					"Tried to initialize UnmanagedNetDataWriter but its already initialized" );
#endif
			_data = ( byte* )AllocationHandler.Malloc( InitialSize );

			Capacity = InitialSize;


			Initialized = true;
		}

		public byte* GetCurrentPointer( int offset ) => _data + Position + offset;

		public byte* GetCurrentPointer() => _data + Position;

		public byte* GetPointer() => _data;

		internal void Reset()
		{
			Position = 0;
		}



		internal void Resize( int newCapacity )
		{
			newCapacity = MathHelper.CeilPower2( newCapacity );
			if ( newCapacity <= Capacity ) return;

			byte* newBuffer = ( byte* )AllocationHandler.Malloc( newCapacity );

			Buffer.MemoryCopy( _data, newBuffer, newCapacity, Capacity );

			AllocationHandler.Free( ( IntPtr )_data );

			Capacity = newCapacity;
			_data = newBuffer;
		}

		internal void CheckAndThrow( int position )
		{

			if ( position >= Capacity )
			{
				throw new IndexOutOfRangeException();
			}
		}


		public void Dispose()
		{
			AllocationHandler.Free( ( IntPtr )_data );
		}

	}

	/// <summary>
	/// Serialize Data to a byte array
	/// </summary>
	[DebuggerTypeProxy( typeof( DebuggerProxy ) )]
	public unsafe struct DataWriter : IDisposable
	{
		private UnmanagedDataWriter* _data;

		/// <summary>
		/// Get the current capacity of the writer
		/// </summary>
		public int Capacity
		{
			get
			{
				Check();
				return _data->Capacity;
			}
		}

		/// <summary>
		/// Get/Set if the DataWriter is disposable
		/// </summary>
		public bool DisposeLocked
		{
			get
			{
				Check();
				return _data->DisposeLocked;
			}
			set
			{
				Check();
				_data->DisposeLocked = value;
			}
		}

		/// <summary>
		/// Get the pointer to the current location in the writer
		/// </summary>
		/// <returns></returns>
		internal byte* GetCurrentBytePointer()
		{
			Check();
			return _data->GetCurrentPointer();
		}


		/// <summary>
		/// Always call Initialize before Using
		/// </summary>
		public void Initialize()
		{
			_data = ( UnmanagedDataWriter* )AllocationHandler.Malloc<UnmanagedDataWriter>();
			*_data = default;
			_data->Setup();
		}

		/// <summary>
		/// Offset the location of the writer
		/// </summary>
		/// <param name="offset"></param>
		public void AddOffset( int offset )
		{
			Check();
			_data->Position += offset;
		}

		/// <summary>
		/// Get the current position of the writer
		/// </summary>
		/// <returns></returns>
		public int GetCurrentPosition()
		{
			Check();
			return _data->Position;
		}

		/// <summary>
		/// Reset the <see cref="DataWriter"/>. This will only move the Location of the writer to 0
		/// </summary>
		public void Reset()
		{
			Check();
			_data->Reset();
		}

		/// <summary>
		/// Check if the data at the given position is accessible
		/// </summary>
		/// <param name="pos"></param>
		public void CheckData( int pos )
		{
			Check();
			ResizeIfNeed( pos );
			_data->CheckAndThrow( pos );
		}

		/// <summary>
		/// Check if the <see cref="DataWriter"/> is valid
		/// </summary>
		public void Check()
		{
#if DEBUG
			if ( !AllocationHandler.AllocationValid( ( IntPtr )_data ) )
			{
				throw new Exception( "Internal writer is not valid" );
			}
#endif
		}

		/// <summary>
		/// Resize if the position is out of bounds
		/// </summary>
		/// <param name="posCompare"></param>
		public void ResizeIfNeed( int posCompare )
		{
			int len = _data->Capacity;
			if ( len <= posCompare )
			{
				while ( len <= posCompare )
				{
					len += 1;
					len *= 2;
				}

				_data->Resize( len );
			}
		}

		/// <summary>
		/// Get the byte pointer to the origin
		/// </summary>
		public byte* OriginBytePointer
		{
			get
			{
				Check();
				return _data->GetPointer();
			}
		}

		/// <summary>
		/// Get the length of the writer
		/// </summary>
		public int Length
		{
			get
			{
				Check();
				return _data->Position;
			}
		}

		private int Position
		{
			get
			{
				Check();
				return _data->Position;
			}

			set
			{
				Check();
				_data->Position = value;
			}
		}

		/// <summary>
		/// Serialize a <see cref="float"/>
		/// </summary>
		public void Put( float value )
		{
			CheckData( Position + 4 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 4;
		}

		/// <summary>
		/// Serialize a <see cref="double"/>
		/// </summary>
		public void Put( double value )
		{
			CheckData( Position + 8 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 8;
		}

		/// <summary>
		/// Serialize a <see cref="long"/>
		/// </summary>
		public void Put( long value )
		{
			CheckData( Position + 8 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 8;
		}

		/// <summary>
		/// Serialize a <see cref="ulong"/>
		/// </summary>
		public void Put( ulong value )
		{
			CheckData( Position + 8 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 8;
		}

		/// <summary>
		/// Serialize a <see cref="int"/>
		/// </summary>
		public void Put( int value )
		{
			CheckData( Position + 4 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 4;
		}

		/// <summary>
		/// Serialize a <see cref="uint"/>
		/// </summary>
		public void Put( uint value )
		{
			CheckData( Position + 4 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 4;
		}

		/// <summary>
		/// Serialize a <see cref="char"/>
		/// </summary>
		public void Put( char value )
		{
			CheckData( Position + 2 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 2;
		}

		/// <summary>
		/// Serialize a <see cref="ushort"/>
		/// </summary>
		public void Put( ushort value )
		{
			CheckData( Position + 2 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 2;
		}

		/// <summary>
		/// Serialize a <see cref="short"/>
		/// </summary>
		public void Put( short value )
		{
			CheckData( Position + 2 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 2;
		}

		/// <summary>
		/// Serialize a <see cref="sbyte"/>
		/// </summary>
		public void Put( sbyte value )
		{
			CheckData( Position + 1 );
			*_data->GetCurrentPointer() = ( byte )value;
			Position++;
		}

		/// <summary>
		/// Serialize a <see cref="byte"/>
		/// </summary>
		public void Put( byte value )
		{
			CheckData( Position + 1 );
			*_data->GetCurrentPointer() = value;
			Position++;
		}

		/// <summary>
		/// Serialize a <see cref="IPEndPoint"/>
		/// </summary>
		public void Put( IPEndPoint endPoint )
		{
			Put( endPoint.Address.ToString() );
			Put( endPoint.Port );
		}

		/// <summary>
		/// Serialize a <see cref="string"/>
		/// </summary>
		public void Put( string value )
		{
			Check();
			if ( string.IsNullOrEmpty( value ) )
			{
				Put( 0 );
				return;
			}

			//put bytes count
			int bytesCount = Encoding.UTF8.GetByteCount( value );
			ResizeIfNeed( Position + bytesCount + 4 );
			Put( bytesCount );

			fixed ( char* charPointer = value )
			{
				//put string
				Encoding.UTF8.GetBytes( charPointer, value.Length, GetCurrentBytePointer(), bytesCount );
			}

			Position += bytesCount;
		}

		/// <summary>
		/// Add bytes to the writer
		/// </summary>
		public void PutBytesWithLength( byte* data, int length )
		{
			Check();
			ResizeIfNeed( length + Position + 4 );
			Put( length );
			Buffer.MemoryCopy( data, GetCurrentBytePointer(), length, length );
			Position += length;
		}

		/// <summary>
		/// Serialize a <see cref="Vector2"/>
		/// </summary>
		public void Put(Vector2 value)
		{
			Put(value.X);
			Put(value.Y);
		}

		/// <summary>
		/// Serialize a <see cref="Vector3"/>
		/// </summary>
		public void Put(Vector3 value)
		{
			Put(value.X);
			Put(value.Y);
			Put(value.Z);
		}

		/// <summary>
		/// Serialize a <see cref="Vector4"/>
		/// </summary>
		public void Put(Vector4 value)
		{
			Put(value.X);
			Put(value.Y);
			Put(value.Z);
			Put(value.W);
		}

		/// <summary>
		/// Serialize a <see cref="Quaternion"/>
		/// </summary>
		public void Put(Quaternion value)
		{
			Put(value.X);
			Put(value.Y);
			Put(value.Z);
			Put(value.W);
		}

		/// <summary>
		/// Serialize a <see cref="Matrix4x4"/>
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


		/// <inheritdoc/>
		public void Dispose()
		{
			if ( _data->DisposeQueued ) return;
			if ( DisposeLocked ) return;

			_data->DisposeQueued = true;
			_data->Dispose();

			AllocationHandler.Free( ( IntPtr )_data );
		}

		/// <summary>
		/// Dispose the <see cref="DataWriter"/> but keep the serialized data
		/// </summary>
		public void DisposeKeepData()
		{
			if ( _data->DisposeQueued ) return;
			if ( DisposeLocked ) throw new InvalidOperationException( "The Serializer Dispose is locked" );

			_data->DisposeQueued = true;
			AllocationHandler.Free( ( IntPtr )_data );
		}

		private struct DebuggerProxy
		{
			private DataWriter writer;
			public DebuggerProxy( DataWriter writer )
			{
				this.writer = writer;
			}

			public byte[] Items
			{
				get
				{
					byte[] result = new byte[writer.Length];
					Marshal.Copy( ( IntPtr )writer.OriginBytePointer, result, 0, result.Length );
					return result;
				}
			}
		}

	}

	internal static unsafe class FastBitConverter
	{
		[StructLayout( LayoutKind.Explicit )]
		private struct ConverterHelperDouble
		{
			[FieldOffset( 0 )] public ulong ALong;

			[FieldOffset( 0 )] public double ADouble;
		}

		[StructLayout( LayoutKind.Explicit )]
		private struct ConverterHelperFloat
		{
			[FieldOffset( 0 )] public int AInt;

			[FieldOffset( 0 )] public float AFloat;
		}

		#region Byte Array Operations

		private static void WriteLittleEndian( byte[] buffer, int offset, ulong data )
		{
			if ( BitConverter.IsLittleEndian )
			{
				buffer[offset] = ( byte )data;
				buffer[offset + 1] = ( byte )( data >> 8 );
				buffer[offset + 2] = ( byte )( data >> 16 );
				buffer[offset + 3] = ( byte )( data >> 24 );
				buffer[offset + 4] = ( byte )( data >> 32 );
				buffer[offset + 5] = ( byte )( data >> 40 );
				buffer[offset + 6] = ( byte )( data >> 48 );
				buffer[offset + 7] = ( byte )( data >> 56 );
			}
			else
			{
				buffer[offset + 7] = ( byte )( data );
				buffer[offset + 6] = ( byte )( data >> 8 );
				buffer[offset + 5] = ( byte )( data >> 16 );
				buffer[offset + 4] = ( byte )( data >> 24 );
				buffer[offset + 3] = ( byte )( data >> 32 );
				buffer[offset + 2] = ( byte )( data >> 40 );
				buffer[offset + 1] = ( byte )( data >> 48 );
				buffer[offset] = ( byte )( data >> 56 );
			}
		}

		private static void WriteLittleEndian( byte[] buffer, int offset, int data )
		{
			if ( BitConverter.IsLittleEndian )
			{
				buffer[offset] = ( byte )( data );
				buffer[offset + 1] = ( byte )( data >> 8 );
				buffer[offset + 2] = ( byte )( data >> 16 );
				buffer[offset + 3] = ( byte )( data >> 24 );
			}
			else
			{
				buffer[offset + 3] = ( byte )( data );
				buffer[offset + 2] = ( byte )( data >> 8 );
				buffer[offset + 1] = ( byte )( data >> 16 );
				buffer[offset] = ( byte )( data >> 24 );
			}
		}

		private static void WriteLittleEndian( byte[] buffer, int offset, short data )
		{
			if ( BitConverter.IsLittleEndian )
			{
				buffer[offset] = ( byte )( data );
				buffer[offset + 1] = ( byte )( data >> 8 );
			}
			else
			{
				buffer[offset + 1] = ( byte )( data );
				buffer[offset] = ( byte )( data >> 8 );
			}
		}

		private static ulong ReadLittleEndian64( byte[] buffer, int position )
		{
			fixed ( byte* numPtr = &buffer[position] )
			{
				return position % 8 == 0
					? *( ulong* )numPtr
					: BitConverter.IsLittleEndian
					? ( uint )( *numPtr | ( numPtr[1] << 8 ) | ( numPtr[2] << 16 ) | ( numPtr[3] << 24 ) ) |
						   ( ( ulong )( numPtr[4] | ( numPtr[5] << 8 ) | ( numPtr[6] << 16 ) | ( numPtr[7] << 24 ) ) << 32 )
					: ( uint )( numPtr[4] << 24 | numPtr[5] << 16 | numPtr[6] << 8 ) | numPtr[7] |
					   ( ( ulong )( *numPtr << 24 | ( numPtr[1] << 16 ) | ( numPtr[2] << 8 ) | numPtr[3] ) << 32 );
			}
		}

		private static int ReadLittleEndian32( byte[] buffer, int position )
		{
			fixed ( byte* numPtr = &buffer[position] )
			{
				if ( position % 4 == 0 ) return *( int* )numPtr;
				return BitConverter.IsLittleEndian
					? *numPtr | ( numPtr[1] << 8 ) | ( numPtr[2] << 16 ) | numPtr[3] << 24
					: *numPtr << 24 | numPtr[1] << 16 | numPtr[2] << 8 | numPtr[3];
			}
		}

		private static short ReadLittleEndian16( byte[] buffer, int position )
		{
			fixed ( byte* numPtr = &buffer[position] )
			{
				if ( position % 2 == 0 ) return *( short* )numPtr;
				if ( BitConverter.IsLittleEndian )
				{
					return ( short )( *numPtr | numPtr[1] << 8 );
				}

				return ( short )( *numPtr << 8 | numPtr[1] );
			}
		}

		public static void WriteBytes( byte[] bytes, int startIndex, double value )
		{
			ConverterHelperDouble ch = new ConverterHelperDouble { ADouble = value };
			WriteLittleEndian( bytes, startIndex, ch.ALong );
		}

		public static double ReadDouble( byte[] bytes, int index )
		{
			ConverterHelperDouble ch = new ConverterHelperDouble { ALong = ReadLittleEndian64( bytes, index ) };
			return ch.ADouble;
		}

		public static void WriteBytes( byte[] bytes, int startIndex, float value )
		{
			ConverterHelperFloat ch = new ConverterHelperFloat { AFloat = value };
			WriteLittleEndian( bytes, startIndex, ch.AInt );
		}

		public static float ReadFloat( byte[] bytes, int index )
		{
			ConverterHelperFloat ch = new ConverterHelperFloat { AInt = ReadLittleEndian32( bytes, index ) };
			return ch.AFloat;
		}

		public static void WriteBytes( byte[] bytes, int startIndex, short value )
		{
			WriteLittleEndian( bytes, startIndex, value );
		}

		public static short ReadShort( byte[] bytes, int index )
		{
			return ReadLittleEndian16( bytes, index );
		}

		public static void WriteBytes( byte[] bytes, int startIndex, ushort value )
		{
			WriteLittleEndian( bytes, startIndex, ( short )value );
		}

		public static ushort ReadUShort( byte[] bytes, int index )
		{
			return ( ushort )ReadLittleEndian16( bytes, index );
		}

		public static void WriteBytes( byte[] bytes, int startIndex, int value )
		{
			WriteLittleEndian( bytes, startIndex, value );
		}

		public static int ReadInt( byte[] bytes, int index )
		{
			return ReadLittleEndian32( bytes, index );
		}

		public static void WriteBytes( byte[] bytes, int startIndex, uint value )
		{
			WriteLittleEndian( bytes, startIndex, ( int )value );
		}

		public static uint ReadUInt( byte[] bytes, int index )
		{
			return ( uint )ReadLittleEndian32( bytes, index );
		}

		public static void WriteBytes( byte[] bytes, int startIndex, long value )
		{
			WriteLittleEndian( bytes, startIndex, ( ulong )value );
		}

		public static long ReadLong( byte[] bytes, int index )
		{
			return ( long )ReadLittleEndian64( bytes, index );
		}

		public static void WriteBytes( byte[] bytes, int startIndex, ulong value )
		{
			WriteLittleEndian( bytes, startIndex, value );
		}

		public static ulong ReadULong( byte[] bytes, int index )
		{
			return ReadLittleEndian64( bytes, index );
		}

		public static char ReadChar( byte[] bytes, int index )
		{
			return ( char )ReadLittleEndian16( bytes, index );
		}

		#endregion

		#region Pointer Operations

		private static void WriteLittleEndian( byte* buffer, ulong data )
		{
			if ( BitConverter.IsLittleEndian )
			{
				buffer[0] = ( byte )data;
				buffer[1] = ( byte )( data >> 8 );
				buffer[2] = ( byte )( data >> 16 );
				buffer[3] = ( byte )( data >> 24 );
				buffer[4] = ( byte )( data >> 32 );
				buffer[5] = ( byte )( data >> 40 );
				buffer[6] = ( byte )( data >> 48 );
				buffer[7] = ( byte )( data >> 56 );
			}
			else
			{
				buffer[7] = ( byte )( data );
				buffer[6] = ( byte )( data >> 8 );
				buffer[5] = ( byte )( data >> 16 );
				buffer[4] = ( byte )( data >> 24 );
				buffer[3] = ( byte )( data >> 32 );
				buffer[2] = ( byte )( data >> 40 );
				buffer[1] = ( byte )( data >> 48 );
				buffer[0] = ( byte )( data >> 56 );
			}
		}

		private static void WriteLittleEndian( byte* buffer, int data )
		{
			if ( BitConverter.IsLittleEndian )
			{
				buffer[0] = ( byte )( data );
				buffer[1] = ( byte )( data >> 8 );
				buffer[2] = ( byte )( data >> 16 );
				buffer[3] = ( byte )( data >> 24 );
			}
			else
			{
				buffer[3] = ( byte )( data );
				buffer[2] = ( byte )( data >> 8 );
				buffer[1] = ( byte )( data >> 16 );
				buffer[0] = ( byte )( data >> 24 );
			}
		}

		private static void WriteLittleEndian( byte* buffer, short data )
		{
			if ( BitConverter.IsLittleEndian )
			{
				buffer[0] = ( byte )( data );
				buffer[1] = ( byte )( data >> 8 );
			}
			else
			{
				buffer[1] = ( byte )( data );
				buffer[0] = ( byte )( data >> 8 );
			}
		}

		private static ulong ReadLittleEndian64( byte* buffer )
		{
			if ( BitConverter.IsLittleEndian )
			{
				return ( uint )( *buffer | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24 ) |
					   ( ulong )( buffer[4] | buffer[5] << 8 | buffer[6] << 16 | buffer[7] << 24 ) << 32;
			}

			return ( uint )( buffer[4] << 24 | buffer[5] << 16 | buffer[6] << 8 ) | buffer[7] |
				   ( ulong )( *buffer << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3] ) << 32;
		}

		private static int ReadLittleEndian32( byte* buffer )
		{
			if ( BitConverter.IsLittleEndian )
			{
				return *buffer | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24;
			}

			return *buffer << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
		}

		private static short ReadLittleEndian16( byte* buffer )
		{
			if ( BitConverter.IsLittleEndian )
			{
				return ( short )( *buffer | buffer[1] << 8 );
			}

			return ( short )( ( *buffer << 8 ) | buffer[1] );
		}

		public static void WriteBytes( byte* bytes, double value )
		{
			ConverterHelperDouble ch = new ConverterHelperDouble { ADouble = value };
			WriteLittleEndian( bytes, ch.ALong );
		}

		public static double ReadDouble( byte* bytes )
		{
			ConverterHelperDouble ch = new ConverterHelperDouble { ALong = ReadLittleEndian64( bytes ) };
			return ch.ADouble;
		}

		public static void WriteBytes( byte* bytes, float value )
		{
			ConverterHelperFloat ch = new ConverterHelperFloat { AFloat = value };
			WriteLittleEndian( bytes, ch.AInt );
		}

		public static float ReadFloat( byte* bytes )
		{
			ConverterHelperFloat ch = new ConverterHelperFloat { AInt = ReadLittleEndian32( bytes ) };
			return ch.AFloat;
		}

		public static void WriteBytes( byte* bytes, short value )
		{
			WriteLittleEndian( bytes, value );
		}

		public static short ReadShort( byte* bytes )
		{
			return ReadLittleEndian16( bytes );
		}

		public static void WriteBytes( byte* bytes, ushort value )
		{
			WriteLittleEndian( bytes, ( short )value );
		}

		public static ushort ReadUShort( byte* bytes )
		{
			return ( ushort )ReadLittleEndian16( bytes );
		}

		public static void WriteBytes( byte* bytes, int value )
		{
			WriteLittleEndian( bytes, value );
		}

		public static int ReadInt( byte* bytes )
		{
			return ReadLittleEndian32( bytes );
		}

		public static void WriteBytes( byte* bytes, uint value )
		{
			WriteLittleEndian( bytes, ( int )value );
		}

		public static uint ReadUInt( byte* bytes )
		{
			return ( uint )ReadLittleEndian32( bytes );
		}

		public static void WriteBytes( byte* bytes, long value )
		{
			WriteLittleEndian( bytes, ( ulong )value );
		}

		public static long ReadLong( byte* bytes )
		{
			return ( long )ReadLittleEndian64( bytes );
		}

		public static void WriteBytes( byte* bytes, ulong value )
		{
			WriteLittleEndian( bytes, value );
		}

		public static ulong ReadULong( byte* bytes )
		{
			return ReadLittleEndian64( bytes );
		}

		public static char ReadChar( byte* bytes )
		{
			return ( char )ReadLittleEndian16( bytes );
		}

		#endregion
	}
}
