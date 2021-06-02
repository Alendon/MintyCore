using System;
using System.Diagnostics;
using System.Net;
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

	[DebuggerTypeProxy( typeof( DebuggerProxy ) )]
	public unsafe struct DataReader : IDisposable
	{
		private readonly UnmanagedDataReader* _internalReader;

		public int RawDataSize
		{
			get
			{
				return _internalReader->DataSize;
			}
		}

		public bool IsNull
		{
			get
			{
				return !_internalReader->IsInitialized || _internalReader->DataSize <= 0;
			}
		}

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

		public bool EndOfData
		{
			get
			{
				return _internalReader->Position >= _internalReader->DataSize;
			}
		}

		public int AvailableBytes
		{
			get
			{
				return _internalReader->DataSize - _internalReader->Position;
			}
		}

		private byte* GetBytePointer()
		{
			return _internalReader->GetCurrentPointer();
		}

		private byte* GetOriginPointer()
		{
			return _internalReader->GetPointer();
		}

		private byte* GetBytePointer( int offset )
		{
			return _internalReader->GetCurrentPointer( offset );
		}

		public DataReader( byte[] source )
		{
			_internalReader = ( UnmanagedDataReader* )AllocationHandler.Malloc<UnmanagedDataReader>();
			*_internalReader = default;

			fixed ( byte* point = source )
			{
				_internalReader->InitializeWithCopy( point, source.Length );
			}
		}

		public DataReader( byte[] source, int position )
		{
			_internalReader = ( UnmanagedDataReader* )AllocationHandler.Malloc<UnmanagedDataReader>();
			*_internalReader = default;

			fixed ( byte* point = source )
			{
				_internalReader->InitializeWithCopy( point, source.Length, position );
			}
		}

		public DataReader( byte* data, int length, int position = 0 )
		{
			_internalReader = ( UnmanagedDataReader* )AllocationHandler.Malloc<UnmanagedDataReader>();
			*_internalReader = default;

			_internalReader->InitializeWithoutCopy( data, length, position );

		}

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

		public byte GetByte()
		{
			CheckAccess( Position );
			byte res = *GetBytePointer();
			Position += 1;
			return res;
		}

		public sbyte GetSByte()
		{
			CheckAccess( Position );
			var res = ( sbyte )*GetBytePointer();
			Position += 1;
			return res;
		}


		public char GetChar()
		{
			CheckAccess( Position + 1 );
			char result = FastBitConverter.ReadChar( GetBytePointer() );
			Position += 2;
			return result;
		}

		public ushort GetUShort()
		{
			CheckAccess( Position + 1 );
			ushort result = FastBitConverter.ReadUShort( GetBytePointer() );
			Position += 2;
			return result;
		}

		public short GetShort()
		{
			CheckAccess( Position + 1 );
			short result = FastBitConverter.ReadShort( GetBytePointer() );
			Position += 2;
			return result;
		}

		public long GetLong()
		{
			CheckAccess( Position + 7 );
			long result = FastBitConverter.ReadLong( GetBytePointer() );
			Position += 8;
			return result;
		}

		public ulong GetULong()
		{
			CheckAccess( Position + 7 );
			ulong result = FastBitConverter.ReadULong( GetBytePointer() );
			Position += 8;
			return result;
		}

		public int GetInt()
		{
			CheckAccess( Position + 3 );
			int result = FastBitConverter.ReadInt( GetBytePointer() );
			Position += 4;
			return result;
		}

		public uint GetUInt()
		{
			CheckAccess( Position + 3 );
			uint result = FastBitConverter.ReadUInt( GetBytePointer() );
			Position += 4;
			return result;
		}

		public float GetFloat()
		{
			CheckAccess( Position + 3 );
			float result = FastBitConverter.ReadFloat( GetBytePointer() );
			Position += 4;
			return result;
		}

		public double GetDouble()
		{
			CheckAccess( Position + 7 );
			double result = FastBitConverter.ReadDouble( GetBytePointer() );
			Position += 8;
			return result;
		}

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

		public DataReader GetBytesAsReader()
		{
			int byteCount = GetInt();
			CheckAccess( Position + byteCount - 1 );
			var pointer = GetBytePointer();
			return new DataReader( pointer, byteCount );
		}

		public (IntPtr data, int length) GetBytesWithoutCopy()
		{
			int byteCount = GetInt();
			CheckAccess( Position + byteCount - 1 );

			byte* data = GetBytePointer();
			Position += byteCount;

			return (new IntPtr( data ), byteCount);
		}

		#endregion

		#region PeekMethods

		public byte PeekByte()
		{
			CheckAccess( Position );
			return *GetBytePointer();
		}

		public sbyte PeekSByte()
		{
			CheckAccess( Position );

			return ( sbyte )*GetBytePointer();
		}

		public bool PeekBool()
		{
			CheckAccess( Position );

			return *GetBytePointer() > 0;
		}

		public char PeekChar()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadChar( GetBytePointer() );
		}

		public ushort PeekUShort()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadUShort( GetBytePointer() );
		}

		public short PeekShort()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadShort( GetBytePointer() );
		}

		public long PeekLong()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadLong( GetBytePointer() );
		}

		public ulong PeekULong()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadULong( GetBytePointer() );
		}

		public int PeekInt()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadInt( GetBytePointer() );
		}

		public uint PeekUInt()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadUInt( GetBytePointer() );
		}

		public float PeekFloat()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadFloat( GetBytePointer() );
		}

		public double PeekDouble()
		{
			CheckAccess( Position );

			return FastBitConverter.ReadDouble( GetBytePointer() );
		}

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



		public void Dispose()
		{

			if ( _internalReader->DisposeQueued ) return;
			_internalReader->DisposeQueued = true;

			_internalReader->Dispose();
			AllocationHandler.Free( ( IntPtr )_internalReader );
		}


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

	[DebuggerTypeProxy( typeof( DebuggerProxy ) )]
	public unsafe struct DataWriter : IDisposable
	{
		private UnmanagedDataWriter* _data;

		public int Capacity
		{
			get
			{
				Check();
				return _data->Capacity;
			}
		}

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

		public void AddOffset( int offset )
		{
			Check();
			_data->Position += offset;
		}

		public int GetCurrentPosition()
		{
			Check();
			return _data->Position;
		}

		public void Reset()
		{
			Check();
			_data->Reset();
		}

		public void CheckData( int pos )
		{
			Check();
			ResizeIfNeed( pos );
			_data->CheckAndThrow( pos );
		}

		public void Check()
		{
#if DEBUG
			if ( !AllocationHandler.AllocationValid( ( IntPtr )_data ) )
			{
				throw new Exception( "Internal writer is not valid" );
			}
#endif
		}


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

		public byte* OriginBytePointer
		{
			get
			{
				Check();
				return _data->GetPointer();
			}
		}


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

		public void Put( float value )
		{
			CheckData( Position + 4 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 4;
		}

		public void Put( double value )
		{
			CheckData( Position + 8 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 8;
		}

		public void Put( long value )
		{
			CheckData( Position + 8 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 8;
		}

		public void Put( ulong value )
		{
			CheckData( Position + 8 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 8;
		}

		public void Put( int value )
		{
			CheckData( Position + 4 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 4;
		}

		public void Put( uint value )
		{
			CheckData( Position + 4 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 4;
		}

		public void Put( char value )
		{
			CheckData( Position + 2 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 2;
		}

		public void Put( ushort value )
		{
			CheckData( Position + 2 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 2;
		}

		public void Put( short value )
		{
			CheckData( Position + 2 );
			FastBitConverter.WriteBytes( GetCurrentBytePointer(), value );
			Position += 2;
		}

		public void Put( sbyte value )
		{
			CheckData( Position + 1 );
			*_data->GetCurrentPointer() = ( byte )value;
			Position++;
		}

		public void Put( byte value )
		{
			CheckData( Position + 1 );
			*_data->GetCurrentPointer() = value;
			Position++;
		}


		public void Put( IPEndPoint endPoint )
		{
			Put( endPoint.Address.ToString() );
			Put( endPoint.Port );
		}

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

		public void PutBytesWithLength( byte* data, int length )
		{
			Check();
			ResizeIfNeed( length + Position + 4 );
			Put( length );
			Buffer.MemoryCopy( data, GetCurrentBytePointer(), length, length );
			Position += length;
		}

		public void Dispose()
		{
			if ( _data->DisposeQueued ) return;
			if ( DisposeLocked ) return;

			_data->DisposeQueued = true;
			_data->Dispose();

			AllocationHandler.Free( ( IntPtr )_data );
		}


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

	public static unsafe class FastBitConverter
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
