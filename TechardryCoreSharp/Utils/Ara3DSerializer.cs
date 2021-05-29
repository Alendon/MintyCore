using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ara3D;

namespace TechardryCoreSharp.Utils
{
	public static class Ara3DSerializer
	{
		#region Int
		public static void Put( this DataWriter writer, Int2 value )
		{
			writer.Put( value.A );
			writer.Put( value.B );
		}

		public static Int2 GetInt2( this DataReader reader )
		{
			return new Int2( reader.GetInt(), reader.GetInt() );
		}

		public static void Put( this DataWriter writer, Int3 value )
		{
			writer.Put( value.A );
			writer.Put( value.B );
			writer.Put( value.C );
		}

		public static Int3 GetInt3( this DataReader reader )
		{
			return new Int3( reader.GetInt(), reader.GetInt(), reader.GetInt() );
		}

		public static void Put( this DataWriter writer, Int4 value )
		{
			writer.Put( value.A );
			writer.Put( value.B );
			writer.Put( value.C );
			writer.Put( value.D );
		}

		public static Int4 GetInt4( this DataReader reader )
		{
			return new Int4( reader.GetInt(), reader.GetInt(), reader.GetInt(), reader.GetInt() );
		}

		#endregion
		#region float

		public static void Put( this DataWriter writer, Vector2 value )
		{
			writer.Put( value.X );
			writer.Put( value.Y );
		}

		public static Vector2 GetVector2( this DataReader reader )
		{
			return new Vector2( reader.GetFloat(), reader.GetFloat() );
		}

		public static void Put( this DataWriter writer, Vector3 value )
		{
			writer.Put( value.X );
			writer.Put( value.Y );
			writer.Put( value.Z );
		}

		public static Vector3 GetVector3( this DataReader reader )
		{
			return new Vector3( reader.GetFloat(), reader.GetFloat(), reader.GetFloat() );
		}

		public static void Put( this DataWriter writer, Vector4 value )
		{
			writer.Put( value.X );
			writer.Put( value.Y );
			writer.Put( value.Z );
			writer.Put( value.W );
		}

		public static Vector4 GetVector4( this DataReader reader )
		{
			return new Vector4( reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat() );
		}

		#endregion

		#region double

		public static void Put( this DataWriter writer, DVector2 value )
		{
			writer.Put( value.X );
			writer.Put( value.Y );
		}

		public static DVector2 GetDVector2( this DataReader reader )
		{
			return new DVector2( reader.GetDouble(), reader.GetDouble() );
		}

		public static void Put( this DataWriter writer, DVector3 value )
		{
			writer.Put( value.X );
			writer.Put( value.Y );
			writer.Put( value.Z );
		}

		public static DVector3 GetDVector3( this DataReader reader )
		{
			return new DVector3( reader.GetDouble(), reader.GetDouble(), reader.GetDouble() );
		}

		public static void Put( this DataWriter writer, DVector4 value )
		{
			writer.Put( value.X );
			writer.Put( value.Y );
			writer.Put( value.Z );
			writer.Put( value.W );
		}

		public static DVector4 GetDVector4( this DataReader reader )
		{
			return new DVector4( reader.GetDouble(), reader.GetDouble(), reader.GetDouble(), reader.GetDouble() );
		}

		#endregion

		public static void Put( this DataWriter writer, Matrix4x4 value )
		{
			writer.Put( value.M11 );
			writer.Put( value.M44 );
			writer.Put( value.M43 );
			writer.Put( value.M41 );
			writer.Put( value.M34 );
			writer.Put( value.M33 );
			writer.Put( value.M32 );
			writer.Put( value.M42 );
			writer.Put( value.M24 );
			writer.Put( value.M23 );
			writer.Put( value.M22 );
			writer.Put( value.M21 );
			writer.Put( value.M14 );
			writer.Put( value.M13 );
			writer.Put( value.M12 );
			writer.Put( value.M31 );
		}

		public static Matrix4x4 GetMatrix4x4( this DataReader reader )
		{
			return new Matrix4x4( reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat() );
		}

		public static void Put(this DataWriter writer, Quaternion value )
		{
			writer.Put( value.X );
			writer.Put( value.Y );
			writer.Put( value.Z );
			writer.Put( value.W );
		}

		public static Quaternion GetQuaternion(this DataReader reader )
		{
			return new Quaternion( reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat() );
		}

	}
}
