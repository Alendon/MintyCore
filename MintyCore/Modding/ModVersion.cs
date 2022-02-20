using System;
using System.Runtime.InteropServices;
using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
///     Struct to handle mod versioning. Mods with the same main/major combination are handled as compatible
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct ModVersion : IEquatable<ModVersion>
{
    /// <summary>
    ///     Main component of the version
    /// </summary>
    [FieldOffset(sizeof(ushort) * 0)] public readonly ushort Main;

    /// <summary>
    ///     Major component of the version
    /// </summary>
    [FieldOffset(sizeof(ushort) * 1)] public readonly ushort Major;

    /// <summary>
    ///     Minor component of the version
    /// </summary>
    [FieldOffset(sizeof(ushort) * 2)] public readonly ushort Minor;

    /// <summary>
    ///     Revision component of the version
    /// </summary>
    [FieldOffset(sizeof(ushort) * 3)] public readonly ushort Revision;

    [FieldOffset(sizeof(ushort) * 0)] private readonly ulong _combined;


    /// <summary>
    ///     Create a new <see cref="ModVersion" />
    /// </summary>
    public ModVersion(ushort main) : this()
    {
        Main = main;
    }

    /// <summary>
    ///     Create a new <see cref="ModVersion" />
    /// </summary>
    public ModVersion(ushort main, ushort major) : this()
    {
        Main = main;
        Major = major;
    }

    /// <summary>
    ///     Create a new <see cref="ModVersion" />
    /// </summary>
    public ModVersion(ushort main, ushort major, ushort minor) : this()
    {
        Main = main;
        Major = major;
        Minor = minor;
    }

    /// <summary>
    ///     Create a new <see cref="ModVersion" />
    /// </summary>
    public ModVersion(ushort main, ushort major, ushort minor, ushort revision)
    {
        _combined = 0;
        Main = main;
        Major = major;
        Minor = minor;
        Revision = revision;
    }

    /// <summary>
    ///     Check if a <see cref="ModVersion" /> is compatible with this.
    /// </summary>
    /// <remarks>
    ///     A Version is handled as compatible when <see cref="ModVersion.Main" /> and <see cref="ModVersion.Major" /> is
    ///     equal
    /// </remarks>
    public bool Compatible(ModVersion other)
    {
        return Main == other.Main && Major == other.Major;
    }


    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ModVersion other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _combined.GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Main}:{Major}:{Minor}:{Revision}";
    }

    /// <inheritdoc />
    public bool Equals(ModVersion other)
    {
        return _combined == other._combined;
    }

    /// <summary>
    ///     Serialize the <see cref="ModVersion" /> to a <see cref="DataWriter" />
    /// </summary>
    public void Serialize(DataWriter writer)
    {
        writer.Put(Main);
        writer.Put(Major);
        writer.Put(Minor);
        writer.Put(Revision);
    }

    /// <summary>
    ///     Deserialize the <see cref="ModVersion" /> from a <see cref="DataReader" />
    /// </summary>
    public static ModVersion Deserialize(DataReader reader)
    {
        return new ModVersion(reader.GetUShort(), reader.GetUShort(), reader.GetUShort(), reader.GetUShort());
    }
}