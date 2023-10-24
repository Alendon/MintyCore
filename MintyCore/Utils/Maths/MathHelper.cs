using System;

namespace MintyCore.Utils.Maths;

/// <summary>
///     Class for misc math functions
/// </summary>
public static class MathHelper
{
    /// <summary>
    ///     Get a number to the next power of 2
    /// </summary>
    public static int CeilPower2(int x)
    {
        if (x < 2) return 1;
        return (int) Math.Pow(2, (int) Math.Log(x - 1, 2) + 1);
    }

    /// <summary>
    ///     Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(byte x, byte mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    ///     Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(sbyte x, sbyte mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    ///     Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(short x, short mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    ///     Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(ushort x, ushort mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    ///     Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(int x, int mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    ///     Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(uint x, uint mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    ///     Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(long x, long mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    ///     Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(ulong x, ulong mask)
    {
        return (x & mask) == mask;
    }
}