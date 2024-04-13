using System;
using Avalonia.Skia;
using SkiaSharp;

namespace MintyCore.AvaloniaIntegration;

/// <inheritdoc />
public class VkSkiaRenderTarget(VkSkiaSurface surface, GRContext grContext) : ISkiaGpuRenderTarget
{
    /// <inheritdoc />
    public ISkiaGpuRenderSession BeginRenderingSession()
    {
        return new VkSkiaGpuRenderSession(surface, grContext);
    }

    /// <inheritdoc />
    public bool IsCorrupted => surface.IsDisposed || grContext.IsAbandoned ||
                               Math.Abs(surface.RenderScaling - surface.RenderScaling) > double.Epsilon;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}