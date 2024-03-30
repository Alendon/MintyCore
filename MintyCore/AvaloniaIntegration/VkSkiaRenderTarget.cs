using System;
using Avalonia.Skia;
using SkiaSharp;

namespace MintyCore.AvaloniaIntegration;

public class VkSkiaRenderTarget(VkSkiaSurface surface, GRContext grContext) : ISkiaGpuRenderTarget
{
    public ISkiaGpuRenderSession BeginRenderingSession()
    {
        return new VkSkiaGpuRenderSession(surface, grContext);
    }

    public bool IsCorrupted => surface.IsDisposed || grContext.IsAbandoned ||
                               Math.Abs(surface.RenderScaling - surface.RenderScaling) > double.Epsilon;

    public void Dispose()
    {
    }
}