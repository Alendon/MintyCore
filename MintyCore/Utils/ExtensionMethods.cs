using System;
using System.Drawing;
using Silk.NET.Vulkan;

namespace MintyCore.Utils;

/// <summary>
/// Misc extension methods
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Check if two versions are compatible
    /// </summary>
    /// <returns>True if compatible</returns>
    public static bool CompatibleWith(this Version version, Version other)
    {
        return version.Major == other.Major && version.Minor == other.Minor;
    }
    
    public static Rect2D ToRect2D(this Rectangle rectangle)
    {
        return new Rect2D
        {
            Offset = new Offset2D(rectangle.X, rectangle.Y),
            Extent = new Extent2D((uint) rectangle.Width, (uint) rectangle.Height)
        };
    }
}