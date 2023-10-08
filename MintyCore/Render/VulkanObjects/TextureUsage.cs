using System;
using MintyCore.Render.Managers;
using Silk.NET.Vulkan;

namespace MintyCore.Render.VulkanObjects;

/// <summary>
///     Enum containing all texture usages
/// </summary>
[Flags]
public enum TextureUsage : byte
{
    /// <summary>
    ///     The Texture can be used as the target of a read-only <see cref="ImageView" />, and can be accessed from a shader.
    /// </summary>
    Sampled = 1 << 0,

    /// <summary>
    ///     The Texture can be used as the target of a read-write <see cref="ImageView" />, and can be accessed from a shader.
    /// </summary>
    Storage = 1 << 1,

    /// <summary>
    ///     The Texture can be used as the color target of a <see cref="Framebuffer" />.
    /// </summary>
    RenderTarget = 1 << 2,

    /// <summary>
    ///     The Texture can be used as the depth target of a <see cref="Framebuffer" />.
    /// </summary>
    DepthStencil = 1 << 3,

    /// <summary>
    ///     The Texture is a two-dimensional cubemap.
    /// </summary>
    Cubemap = 1 << 4,

    /// <summary>
    ///     The Texture is used as a read-write staging resource for uploading Texture data.
    ///     With this flag, a Texture can be mapped using the <see cref="MemoryManager.Map" />
    ///     method.
    /// </summary>
    Staging = 1 << 5
}