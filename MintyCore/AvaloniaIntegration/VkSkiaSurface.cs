using System;
using Avalonia.Skia;
using MintyCore.Graphics.VulkanObjects;
using SkiaSharp;

namespace MintyCore.AvaloniaIntegration;

/// <inheritdoc />
public class VkSkiaSurface : ISkiaSurface
{
    /// <inheritdoc />
    public SKSurface Surface { get; }
    
    /// <summary>
    /// The texture that the surface renders to.
    /// </summary>
    public Texture Texture { get; }


    /// <inheritdoc />
    public bool CanBlit => false;
    /// <summary>
    /// The scaling factor for rendering.
    /// </summary>
    public double RenderScaling { get; set; }
    
    
    /// <summary>
    ///  Whether the surface has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Creates a new instance of <see cref="VkSkiaSurface"/>.
    /// </summary>
    /// <param name="skSurface"> The skia surface. </param>
    /// <param name="texture"> The texture to render to. </param>
    /// <param name="renderScaling"> The scaling factor for rendering. </param>
    public VkSkiaSurface(SKSurface skSurface, Texture texture, double renderScaling)
    {
        Surface = skSurface;
        Texture = texture;
        RenderScaling = renderScaling;
    }


    /// <inheritdoc />
    public void Blit(SKCanvas canvas)
    {
        throw new NotSupportedException();
    }


    /// <inheritdoc />
    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;

        Surface.Dispose();
        Texture.Dispose();
    }
}