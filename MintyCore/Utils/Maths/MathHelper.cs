using System;
using System.Numerics;
using MintyCore.UI;

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
    
    /// <summary>
    /// Check whether or two layouts overlaps
    /// </summary>
    public static bool Overlaps(Layout first, Layout second)
    {
        //Construct the top left and bottom right coordinates of the rectangles
        var firstLeftUp = new Vector2(first.Offset.X, first.Offset.Y + first.Extent.Y);
        var secondLeftUp = new Vector2(second.Offset.X, second.Offset.Y + second.Extent.Y);
        var firstRightDown = new Vector2(first.Offset.X + first.Extent.X, first.Offset.Y);
        var secondRightDown = new Vector2(second.Offset.X + second.Extent.X, second.Offset.Y);

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

    /// <summary>
    /// Check if one layout contains another
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="child"></param>
    /// <returns></returns>
    public static bool Contains(Layout parent, Layout child)
    {
        return parent.Offset.X <= child.Offset.X && (parent.Offset.Y <= child.Offset.Y) &&
               (parent.Offset.X + parent.Extent.X) >= (child.Offset.X + child.Extent.X) &&
               parent.Offset.Y + parent.Extent.Y >= child.Offset.Y + child.Extent.Y;
    }

    /// <summary>
    /// Check if a point is in a rectangle
    /// </summary>
    /// <param name="rectangle"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static bool InRectangle(Layout rectangle, Vector2 point)
    {
        return point.X > rectangle.Offset.X && point.X < rectangle.Offset.X + rectangle.Extent.X &&
               point.Y > rectangle.Offset.Y && point.Y < rectangle.Offset.Y + rectangle.Extent.Y;
    }
}