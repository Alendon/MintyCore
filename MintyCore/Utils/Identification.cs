using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MintyCore.Registries;

namespace MintyCore.Utils
{
	/// <summary>
	///     Struct to identify everything
	/// </summary>
	[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    [StructLayout(LayoutKind.Explicit)]
    public struct Identification : IEquatable<Identification>
    {
	    /// <summary>
	    ///     ModId of this object
	    /// </summary>
	    [FieldOffset(0)] private ushort Mod;

	    /// <summary>
	    ///     CategoryId of this object
	    /// </summary>
	    [FieldOffset(sizeof(ushort))] private ushort Category;

	    /// <summary>
	    ///     Incremental ObjectId (by mod and category)
	    /// </summary>
	    [FieldOffset(sizeof(ushort) * 2)] private uint Object;

        [FieldOffset(0)] internal readonly ulong numeric;

        internal Identification(ushort mod, ushort category, uint @object)
        {
            numeric = 0;
            Mod = mod;
            Category = category;
            Object = @object;
        }

        /// <summary>
        ///     Serialize the <see cref="Identification" />
        /// </summary>
        public void Serialize(DataWriter writer)
        {
            writer.Put(Mod);
            writer.Put(Category);
            writer.Put(Object);
        }

        /// <summary>
        ///     Deserialize the <see cref="Identification" />
        /// </summary>
        public void Deserialize(DataReader reader)
        {
            Mod = reader.GetUShort();
            Category = reader.GetUShort();
            Object = reader.GetUInt();
        }

        /// <summary>
        ///     Invalid <see cref="Identification" />
        /// </summary>
        public static Identification Invalid => new(Constants.InvalidId, Constants.InvalidId, Constants.InvalidId);

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is Identification identification && Equals(identification);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public bool Equals(Identification other)
        {
            return numeric == other.numeric;
        }

        /// <summary>
        ///     Operator to check if two <see cref="Identification" /> are equal
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Identification left, Identification right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///     Operator to check if two <see cref="Identification" /> are equal
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Identification left, Identification right)
        {
            return !(left == right);
        }

        /// <summary>
        ///     Operator to check if two <see cref="Identification" /> are equal
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return numeric.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return
                $"{RegistryManager.GetModStringId(Mod)}:{RegistryManager.GetCategoryStringId(Category)}:{RegistryManager.GetObjectStringId(Mod, Category, Object)}";
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}