using System;
using Avalonia.Platform;

namespace MintyCore.AvaloniaIntegration;

/// <inheritdoc />
public class VkPlatformGraphics : IPlatformGraphics
{
    private readonly VkSkiaGpu _gpu;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gpu"></param>
    public VkPlatformGraphics(VkSkiaGpu gpu)
    {
        _gpu = gpu;
    }

    /// <inheritdoc />
    public IPlatformGraphicsContext CreateContext()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///  Get the shared context.
    /// </summary>
    /// <returns></returns>
    public VkSkiaGpu GetSharedContext()
    {
        return _gpu;
    }

    IPlatformGraphicsContext IPlatformGraphics.GetSharedContext()
    {
        return GetSharedContext();
    }

    /// <inheritdoc />
    public bool UsesSharedContext => true;
}