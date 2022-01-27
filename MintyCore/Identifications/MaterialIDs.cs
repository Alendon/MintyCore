using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
///     <see langword="static" /> partial class which contains all <see cref="Render.Material" /> ids
/// </summary>
public static class MaterialIDs
{
    /// <summary>
    ///     <see cref="Identification" /> of the ColorMaterial
    /// </summary>
    public static Identification Color { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the GroundMaterial
    /// </summary>
    public static Identification Ground { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the TriangleMaterial
    /// </summary>
    public static Identification Triangle { get; set; }
    public static Identification UiOverlay { get; internal set; }
}