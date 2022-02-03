using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
///     <see langword="static" /> partial class which contains all <see cref="Render.Texture" /> ids
/// </summary>
public static class TextureIDs
{
    /// <summary>
    ///     <see cref="Identification" /> of the Ground <see cref="Render.Texture" />
    /// </summary>
    public static Identification Ground { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the Dirt <see cref="Render.Texture" />
    /// </summary>
    public static Identification Dirt { get; internal set; }
}