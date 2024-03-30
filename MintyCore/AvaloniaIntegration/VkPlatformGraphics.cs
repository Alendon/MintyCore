using System;
using Avalonia.Platform;

namespace MintyCore.AvaloniaIntegration;

public class VkPlatformGraphics : IPlatformGraphics
{
    private readonly VkSkiaGpu _gpu;

    public VkPlatformGraphics(VkSkiaGpu gpu)
    {
        _gpu = gpu;
    }

    public IPlatformGraphicsContext CreateContext()
    {
        throw new NotSupportedException();
    }

    public VkSkiaGpu GetSharedContext()
    {
        return _gpu;
    }

    IPlatformGraphicsContext IPlatformGraphics.GetSharedContext()
    {
        return GetSharedContext();
    }

    public bool UsesSharedContext => true;
}