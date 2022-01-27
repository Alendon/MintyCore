using System;
using System.Numerics;
using Silk.NET.Vulkan;

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
        return (int)Math.Pow(2, (int)Math.Log(x - 1, 2) + 1);
    }

    /// <summary>
    /// Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(byte x, byte mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    /// Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(sbyte x, sbyte mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    /// Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(short x, short mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    /// Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(ushort x, ushort mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    /// Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(int x, int mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    /// Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(uint x, uint mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    /// Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(long x, long mask)
    {
        return (x & mask) == mask;
    }

    /// <summary>
    /// Check whether one or multiple bits are set by the given bitmask
    /// </summary>
    public static bool IsBitSet(ulong x, ulong mask)
    {
        return (x & mask) == mask;
    }

    public static bool Overlaps(Rect2D first, Rect2D second)
    {
        //Construct the top left and bottom right coordinates of the rectangles
        var firstLeftUp = new Vector2(first.Offset.X, first.Offset.Y + first.Extent.Height);
        var secondLeftUp = new Vector2(second.Offset.X, second.Offset.Y + second.Extent.Height);
        var firstRightDown = new Vector2(first.Offset.X + first.Extent.Width, first.Offset.Y);
        var secondRightDown = new Vector2(second.Offset.X + second.Extent.Width, second.Offset.Y);

        if (firstLeftUp.X >= secondRightDown.X || secondLeftUp.X >= firstRightDown.X)
        {
            return false;
        }

        if (firstRightDown.Y >= secondLeftUp.Y || secondRightDown.Y >= firstLeftUp.Y)
        {
            return false;
        }

        return true;
    }

    public static bool Contains(Rect2D parent, Rect2D child)
    {
        return parent.Offset.X <= child.Offset.X && (parent.Offset.Y <= child.Offset.Y) &&
               (parent.Offset.X + parent.Extent.Width) >= (child.Offset.X + child.Extent.Width) &&
               parent.Offset.Y + parent.Extent.Height >= child.Offset.Y + child.Extent.Height;
    }

    public static bool InRectangle(Rect2D rectangle, Vector2 point)
    {
        return point.X > rectangle.Offset.X && point.X < rectangle.Offset.X + rectangle.Extent.Width &&
               point.Y > rectangle.Offset.Y && point.Y < rectangle.Offset.Y + rectangle.Extent.Height;
    }
}