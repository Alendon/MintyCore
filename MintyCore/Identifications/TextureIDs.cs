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

    public static Identification UiBorderLeft { get; set; }
    public static Identification UiBorderRight { get; set; }
    public static Identification UiBorderTop { get; set; }
    public static Identification UiBorderBottom { get; set; }
    public static Identification UiCornerUpperLeft { get; set; }
    public static Identification UiCornerUpperRight { get; set; }
    public static Identification UiCornerLowerLeft { get; set; }
    public static Identification UiCornerLowerRight { get; set; }
    public static Identification Dirt { get; set; }
}