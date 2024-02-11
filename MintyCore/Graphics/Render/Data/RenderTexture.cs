using System;
using MintyCore.Graphics.VulkanObjects;
using OneOf;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render.Data;

public record RenderTextureDescription(
    OneOf<Func<Extent2D>, Swapchain> Dimensions,
    OneOf<Format, Swapchain> Format,
    TextureUsage Usage = TextureUsage.RenderTarget | TextureUsage.Sampled,
    ClearColorValue? ClearColorValue = null
);

public struct Swapchain;