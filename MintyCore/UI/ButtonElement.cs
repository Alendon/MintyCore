using System;
using MintyCore.Render;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MintyCore.UI;

public class ButtonElement : Element
{
    private Image<Rgba32> normal;
    private Image<Rgba32> hovered;

    public event Action OnLeftClickCb = delegate { };

    public ButtonElement(Layout layout) : base(layout)
    {
        
    }

    public override void Initialize()
    {
        normal = BorderBuilder.BuildBorderedImage((int)PixelSize.X, (int)PixelSize.Y,
            new Rgba32(0, 0, 0, byte.MaxValue));
        hovered = BorderBuilder.BuildBorderedImage((int)PixelSize.X, (int)PixelSize.Y,
            new Rgba32(128, 128, 128, byte.MaxValue));
        HasChanged = true;
    }

    public override void Resize()
    {
        normal.Dispose();
        hovered.Dispose();
        Initialize();
    }

    private bool lastHoveredState;

    public override Image<Rgba32> Image => CursorHovering ? hovered : normal;

    public override void Update(float deltaTime)
    {
        HasChanged = lastHoveredState != CursorHovering;
        lastHoveredState = CursorHovering;
    }


    public override void OnLeftClick()
    {
        if (!CursorHovering) return;
        OnLeftClickCb();
    }

    public override void Dispose()
    {
        base.Dispose();
        hovered.Dispose();
        normal.Dispose();
    }
}