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

	/// <summary>
	/// Struct to identify everything
	/// </summary>
	[DebuggerDisplay( "{" + nameof( GetDebuggerDisplay ) + "(),nq}" )]
	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct Identification : IEquatable<Identification>
	{
		/// <summary>
		/// ModId of this object
		/// </summary>
		[FieldOffset( 0 )]
		public ushort Mod;
		/// <summary>
		/// CategoryId of this object
		/// </summary>
		[FieldOffset( sizeof(ushort))]
		public ushort Category;
		/// <summary>
		/// Incremental ObjectId (by mod and category)
		/// </summary>
		[FieldOffset(sizeof(ushort) * 2)]
		public uint Object;

		[FieldOffset(0)]
		ulong numeric;

		internal Identification( ushort mod, ushort category, uint @object )
		{
			numeric = 0;
			Mod = mod;
			Category = category;
			Object = @object;
		}

		/// <summary>
		/// Invalid <see cref="Identification"/>
		/// </summary>
		public static Identification Invalid => new Identification(Constants.InvalidID,Constants.InvalidID,Constants.InvalidID);

		/// <inheritdoc/>
		public override bool Equals( object? obj ) => obj is Identification identification && Equals( identification );

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		public unsafe bool Equals( Identification other )
		{
			return numeric == other.numeric;
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		public static bool operator ==( Identification left, Identification right ) => left.Equals( right );
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		public static bool operator !=( Identification left, Identification right ) => !( left == right );
		
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		public unsafe override int GetHashCode()
		{
			return numeric.GetHashCode();
		}

		/// <inheritdoc/>
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
