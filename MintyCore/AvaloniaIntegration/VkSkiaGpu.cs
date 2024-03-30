using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Skia;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace MintyCore.AvaloniaIntegration;

public class VkSkiaGpu : ISkiaGpu
{
    GRContext _grContext;
    private readonly IVulkanEngine _vulkanEngine;
    private readonly ITextureManager _textureManager;

    public VkSkiaGpu(IVulkanEngine vulkanEngine, ITextureManager textureManager)
    {
        _vulkanEngine = vulkanEngine;
        _textureManager = textureManager;

        var vkContext = new GRVkBackendContext()
        {
            VkInstance = vulkanEngine.Instance.Handle,
            VkDevice = vulkanEngine.Device.Handle,
            VkPhysicalDevice = vulkanEngine.PhysicalDevice.Handle,
            VkQueue = vulkanEngine.GraphicQueue.queue.Handle,
            GraphicsQueueIndex = vulkanEngine.GraphicQueue.familyIndex,
            GetProcedureAddress = GetProc,
            ProtectedContext = false,
            MaxAPIVersion = Vk.Version13.Value
            //TODO: Add support for passing enabled extensions and features
        };

        if (GRContext.CreateVulkan(vkContext) is not { } grContext)
            throw new InvalidOperationException("Couldn't create Vulkan context");

        _grContext = grContext;
        return;

        IntPtr GetProc(string name, IntPtr instance, IntPtr device) =>
            device != IntPtr.Zero
                ? vulkanEngine.Vk.GetDeviceProcAddr(new Device(device), name)
                : vulkanEngine.Vk.GetInstanceProcAddr(new Instance(instance), name);
    }

    public VkSkiaSurface CreateSurface(PixelSize size, double renderScaling)
    {
        size = new PixelSize(Math.Max(size.Width, 1), Math.Max(size.Height, 1));

        var texDesc = TextureDescription.Texture2D((uint)size.Width, (uint)size.Height, 1, 1, Format.R8G8B8A8Unorm,
            TextureUsage.RenderTarget);

        var texture = _textureManager.Create(ref texDesc);

        var cb = _vulkanEngine.GetSingleTimeCommandBuffer();
        texture.TransitionImageLayout(cb, 0, 1, 0, 1, ImageLayout.ColorAttachmentOptimal);

        var imageInfo = new GRVkImageInfo()
        {
            CurrentQueueFamily = _vulkanEngine.GraphicQueue.familyIndex,
            Format = (uint)texture.Format,
            Image = texture.Image.Handle,
            ImageLayout = (uint)ImageLayout.ColorAttachmentOptimal,
            LevelCount = 1,
            ImageUsageFlags = (uint)texture.ImageUsageFlags,
            ImageTiling = (uint)ImageTiling.Optimal,
            SampleCount = 1,
            Protected = false,
            SharingMode = (uint)SharingMode.Exclusive
        };

        var skSurface = SKSurface.Create(_grContext, new GRBackendRenderTarget(size.Width, size.Height, 1, imageInfo),
            GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888, new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal));

        if (skSurface is null)
            throw new InvalidOperationException("Couldn't create Skia surface from Vulkan image");

        return new VkSkiaSurface(skSurface, texture, renderScaling);
    }


    public object? TryGetFeature(Type featureType)
    {
        return null;
    }

    public IDisposable EnsureCurrent()
    {
        return EmptyDisposable.Instance;
    }

    public bool IsLost => _grContext.IsAbandoned;

    public ISkiaGpuRenderTarget? TryCreateRenderTarget(IEnumerable<object> surfaces)
        => surfaces.OfType<VkSkiaSurface>().FirstOrDefault() is { } surface
            ? new VkSkiaRenderTarget(surface, _grContext)
            : null;

    public ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession? session)
    {
        if (session is not VkSkiaGpuRenderSession renderSession)
            return null;

        return CreateSurface(size, renderSession.ScaleFactor);
    }

    public void Dispose()
    {
        _grContext.Dispose();
    }
}