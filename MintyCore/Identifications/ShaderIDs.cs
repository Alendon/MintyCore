using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
///     <see langword="static" /> partial class which contains all <see cref="Silk.NET.Vulkan.ShaderModule" /> ids
/// </summary>
public static class ShaderIDs
{
    /// <summary>
    ///     <see cref="Identification" /> of the ColorFrag <see cref="Silk.NET.Vulkan.ShaderModule" />
    /// </summary>
    public static Identification ColorFrag { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the CommonVert <see cref="Silk.NET.Vulkan.ShaderModule" />
    /// </summary>
    public static Identification CommonVert { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the WireframeFrag <see cref="Silk.NET.Vulkan.ShaderModule" />
    /// </summary>
    public static Identification WireframeFrag { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the Texture <see cref="Silk.NET.Vulkan.ShaderModule" />
    /// </summary>
    public static Identification Texture { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the TriangleVert <see cref="Silk.NET.Vulkan.ShaderModule" />
    /// </summary>
    public static Identification TriangleVert { get; set; }

    /// <summary>
    ///     <see cref="Identification" /> of the UiOverlayVert <see cref="Silk.NET.Vulkan.ShaderModule" />
    /// </summary>
    public static Identification UiOverlayVert { get; set; }
    
    /// <summary>
    ///     <see cref="Identification" /> of the UiOverlayFrag <see cref="Silk.NET.Vulkan.ShaderModule" />
    /// </summary>
    public static Identification UiOverlayFrag { get; set; }
}