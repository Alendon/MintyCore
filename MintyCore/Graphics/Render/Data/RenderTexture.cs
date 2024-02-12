using System;
using MintyCore.Graphics.VulkanObjects;
using OneOf;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render.Data;

/// <summary>
/// Record to describe a render texture
/// </summary>
/// <param name="Dimensions"> The dimensions of the texture </param>
/// <param name="Format"> The format of the texture </param>
/// <param name="Usage"> The usage of the texture </param>
/// <param name="ClearColorValue" > The clear color value of the texture </param>
public record RenderTextureDescription(
    OneOf<Func<Extent2D>, Swapchain> Dimensions,
    OneOf<Format, Swapchain> Format,
    TextureUsage Usage = TextureUsage.RenderTarget | TextureUsage.Sampled,
    ClearColorValue? ClearColorValue = null
);

/// <summary>
/// Placeholder struct to represent that the swapchain should be used as reference
/// </summary>
public struct Swapchain;