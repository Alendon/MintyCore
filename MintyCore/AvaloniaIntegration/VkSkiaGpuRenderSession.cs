using Avalonia.Skia;
using SkiaSharp;

namespace MintyCore.AvaloniaIntegration;

public class VkSkiaGpuRenderSession : ISkiaGpuRenderSession
{
    public VkSkiaSurface Surface { get; }
    
    public GRContext GrContext { get; }
    public SKSurface SkSurface => Surface.Surface;
    public double ScaleFactor => Surface.RenderScaling;
    public GRSurfaceOrigin SurfaceOrigin => GRSurfaceOrigin.TopLeft;

    public VkSkiaGpuRenderSession(VkSkiaSurface surface, GRContext grContext)
    {
        Surface = surface;
        GrContext = grContext;
    }

    public void Dispose()
    {
        Surface.Surface.Flush();
    }
}