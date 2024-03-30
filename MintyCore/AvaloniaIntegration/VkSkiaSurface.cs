using System;
using Avalonia.Skia;
using MintyCore.Graphics.VulkanObjects;
using SkiaSharp;

namespace MintyCore.AvaloniaIntegration;

public class VkSkiaSurface : ISkiaSurface
{
    public SKSurface Surface { get; }
    public Texture Texture { get; }


    public bool CanBlit => false;
    public double RenderScaling { get; set; }
    public bool IsDisposed { get; private set; }

    public VkSkiaSurface(SKSurface skSurface, Texture texture, double renderScaling)
    {
        Surface = skSurface;
        Texture = texture;
        RenderScaling = renderScaling;
    }


    public void Blit(SKCanvas canvas)
    {
        throw new NotSupportedException();
    }


    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;

        Surface.Dispose();
        Texture.Dispose();
    }
}