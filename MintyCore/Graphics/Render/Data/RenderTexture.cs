using System;
using MintyCore.Graphics.VulkanObjects;
using OneOf;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render.Data;

public record RenderTextureDescription(
    OneOf<Func<Extent2D>, Swapchain> dimensions,
    OneOf<Format, Swapchain> format,
    TextureUsage usage = TextureUsage.RenderTarget | TextureUsage.Sampled,
    ClearColorValue? clearColorValue = null
);

public struct Swapchain;