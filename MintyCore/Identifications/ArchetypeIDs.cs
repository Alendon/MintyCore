using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
///     <see langword="static" /> partial class which contains all archetype ids
/// </summary>
public static class ArchetypeIDs
{
    /// <summary>
    ///     <see cref="Identification" /> of the mesh archetype
    /// </summary>
    public static Identification Mesh { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the player archetype
    /// </summary>
    public static Identification Player { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the rigid body archetype
    /// </summary>
    public static Identification RigidBody { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the test render archetype
    /// </summary>
    public static Identification TestRender { get; set; }
}