using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Identifications;

/// <summary>
///     <see langword="static" /> partial class which contains all <see cref="Render.Material" /> ids
/// </summary>
public static class MaterialIDs
{
    /// <summary>
    ///     <see cref="Identification" /> of the GroundMaterial
    /// </summary>
    public static Identification Ground { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the TriangleMaterial
    /// </summary>
    public static Identification Triangle { get; set; }

    /// <summary>
    ///     <see cref="Identification" /> of the ui overlay material
    /// </summary>
    public static Identification UiOverlay { get; internal set; }

    /// <summary>
    /// <see cref="Identification" /> of a <see cref="System.Func{Identification, DescriptorSet}"/> that returns a, from the <see cref="TextureHandler"/> auto generated <see cref="DescriptorType.SampledImage"/> <see cref="DescriptorSet"/> to be used in a <see cref="Material"/>
    /// </summary>
    public static Identification TextureFetch { get; internal set; }
}