using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MintyCore.Utils;

/// <summary>
///     Struct to identify everything
/// </summary>
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct Identification : IEquatable<Identification>
{
    /// <summary>
    ///     ModId of this object
    /// </summary>
    [FieldOffset(0)] public readonly ushort Mod;

    /// <summary>
    ///     CategoryId of this object
    /// </summary>
    [FieldOffset(sizeof(ushort))] public readonly ushort Category;

    /// <summary>
    ///     Incremental ObjectId (by mod and category)
    /// </summary>
    [FieldOffset(sizeof(ushort) * 2)] public readonly uint Object;

    internal Identification(ushort mod, ushort category, uint @object)
    {
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
    /// <returns>True if deserialization was successful</returns>
    public static bool Deserialize(DataReader reader, out Identification identification)
    {
        var successful = reader.TryGetUShort(out var mod);
        successful &= reader.TryGetUShort(out var category);
        successful &= reader.TryGetUInt(out var @object);
        identification = new Identification(mod, category, @object);

        return successful;
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
        return Mod == other.Mod && Category == other.Category && Object == other.Object;
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
        return HashCode.Combine(Mod, Category, Object);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        //TODO add string formatting to use for debugging display

        return $"{Mod}:{Category}:{Object}";
    }

    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}