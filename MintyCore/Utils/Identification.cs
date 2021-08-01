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
	public struct Identification : IEquatable<Identification>
	{
		/// <summary>
		/// ModId of this object
		/// </summary>
		public ushort Mod;
		/// <summary>
		/// CategoryId of this object
		/// </summary>
		public ushort Category;
		/// <summary>
		/// Incremental ObjectId (by mod and category)
		/// </summary>
		public uint Object;

		internal Identification( ushort mod, ushort category, uint @object )
		{
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
		public bool Equals( Identification other ) => Mod == other.Mod && Category == other.Category && Object == other.Object;

		/// <inheritdoc/>
		public static bool operator ==( Identification left, Identification right ) => left.Equals( right );
		/// <inheritdoc/>
		public static bool operator !=( Identification left, Identification right ) => !( left == right );
		
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		public unsafe override int GetHashCode()
		{
			Identification current = this;
			return ((long*)&current)->GetHashCode();
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
