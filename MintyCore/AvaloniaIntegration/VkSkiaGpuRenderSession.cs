using Avalonia.Skia;
using SkiaSharp;

namespace MintyCore.AvaloniaIntegration;

/// <inheritdoc />
public class VkSkiaGpuRenderSession : ISkiaGpuRenderSession
{
    /// <summary>
    ///  The surface for this render session.
    /// </summary>
    public VkSkiaSurface Surface { get; }

    /// <inheritdoc />
    public GRContext GrContext { get; }

    /// <inheritdoc />
    public SKSurface SkSurface => Surface.Surface;

    /// <inheritdoc />
    public double ScaleFactor => Surface.RenderScaling;

    /// <inheritdoc />
    public GRSurfaceOrigin SurfaceOrigin => GRSurfaceOrigin.TopLeft;

    /// <summary>
    ///  Creates a new instance of <see cref="VkSkiaGpuRenderSession"/>.
    /// </summary>
    public VkSkiaGpuRenderSession(VkSkiaSurface surface, GRContext grContext)
    {
        Surface = surface;
        GrContext = grContext;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Surface.Surface.Flush();
    }
}