using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Registries;

namespace MintyCore.Utils
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

		public static Identification Invalid => default;

		public override bool Equals( object? obj ) => obj is Identification identification && Equals( identification );

		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		public bool Equals( Identification other ) => Mod == other.Mod && Category == other.Category && Object == other.Object;

		public static bool operator ==( Identification left, Identification right ) => left.Equals( right );
		public static bool operator !=( Identification left, Identification right ) => !( left == right );

		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		public unsafe override int GetHashCode()
		{
			Identification current = this;
			return ((long*)&current)->GetHashCode();
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
