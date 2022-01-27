using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
///     <see langword="static" /> partial class which contains all <see cref="Render.Mesh" /> ids
/// </summary>
public static class MeshIDs
{
    /// <summary>
    ///     <see cref="Identification" /> of the Suzanne mesh
    /// </summary>
    public static Identification Suzanne { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the Square mesh
    /// </summary>
    public static Identification Square { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the Capsule mesh
    /// </summary>
    public static Identification Capsule { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the Cube mesh
    /// </summary>
    public static Identification Cube { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the Sphere mesh
    /// </summary>
    public static Identification Sphere { get; internal set; }
}