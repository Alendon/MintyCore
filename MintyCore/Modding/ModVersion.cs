using System;
using System.Runtime.InteropServices;
using MintyCore.Utils;

namespace MintyCore.Modding
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ModVersion : IEquatable<ModVersion>
    {
        [FieldOffset(sizeof(ushort) * 0)]
        public readonly ushort Main;
        [FieldOffset(sizeof(ushort) * 1)]
        public readonly ushort Major;
        [FieldOffset(sizeof(ushort) * 2)]
        public readonly ushort Minor;
        [FieldOffset(sizeof(ushort) * 3)]
        public readonly ushort Revision;

        [FieldOffset(sizeof(ushort) * 0)]
        private readonly ulong _combined;


        public ModVersion(ushort main) : this()
        {
            Main = main;
        }

        public ModVersion(ushort main, ushort major) : this()
        {
            Main = main;
            Major = major;
        }

        public ModVersion(ushort main, ushort major, ushort minor) : this()
        {
            Main = main;
            Major = major;
            Minor = minor;
        }

        public ModVersion(ushort main, ushort major, ushort minor, ushort revision)
        {
            _combined = 0;
            Main = main;
            Major = major;
            Minor = minor;
            Revision = revision;
        }

        public bool Compatible(ModVersion other)
        {
            return Main == other.Main && Major == other.Major;
        }

        public override bool Equals(object? obj)
        {
            return obj is ModVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _combined.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Main}:{Major}:{Minor}:{Revision}";
        }

        public bool Equals(ModVersion other)
        {
            return _combined == other._combined;
        }

        public void Serialize(DataWriter writer)
        {
            writer.Put(Main);
            writer.Put(Major);
            writer.Put(Minor);
            writer.Put(Revision);
        }

        public static ModVersion Deserialize(DataReader reader)
        {
            return new ModVersion(reader.GetUShort(), reader.GetUShort(), reader.GetUShort(), reader.GetUShort());
        }
    }
}