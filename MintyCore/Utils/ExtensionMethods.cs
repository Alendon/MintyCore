using System;

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
}