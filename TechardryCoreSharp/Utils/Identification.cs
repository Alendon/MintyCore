using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Registries;

namespace TechardryCoreSharp.Utils
{
	[DebuggerDisplay( "{" + nameof( GetDebuggerDisplay ) + "(),nq}" )]
	public struct Identification : IEquatable<Identification>
	{
		public ushort Mod;
		public ushort Category;
		public uint Object;

		public Identification( ushort mod, ushort category, uint @object )
		{
			Mod = mod;
			Category = category;
			Object = @object;
		}

		public override bool Equals( object obj ) => obj is Identification identification && Equals( identification );
		public bool Equals( Identification other ) => Mod == other.Mod && Category == other.Category && Object == other.Object;

		public static bool operator ==( Identification left, Identification right ) => left.Equals( right );
		public static bool operator !=( Identification left, Identification right ) => !( left == right );

		public unsafe override int GetHashCode()
		{
			int returnVal;
			fixed ( ushort* mod = &Mod )
			fixed ( ushort* category = &Category )
			fixed ( uint* @object = &Object )
			{
				returnVal = ( *( short* )mod << 16 ) + *( short* )category;
				returnVal ^= *( int* )@object;
			}
			return returnVal;
		}

		public override string ToString()
		{
			return $"{RegistryManager.GetModStringID(Mod)}:{RegistryManager.GetCategoryStringID(Category)}:{RegistryManager.GetObjectStringID(Mod, Category, Object)}";
		}

		private string GetDebuggerDisplay()
		{
			return ToString();
		}
	}
}
