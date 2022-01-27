using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
/// Class containing all <see cref="Silk.NET.Vulkan.DescriptorSetLayout"/> <see cref="Identification"/>
/// </summary>
public static class DescriptorSetIDs
{
    /// <summary>
    ///     <see cref="Identification"/> for the CameraBuffer layout
    /// </summary>
    public static Identification CameraBuffer { get; set; }

    /// <summary>
    ///     <see cref="Identification"/> for the Texture layout
    /// </summary>
    public static Identification SampledTexture { get; set; }
}