using System;
using MintyCore.Render;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;

namespace MintyCore.UI;

public class ButtonElement : Element
{
    private Texture normal;
    private Texture hovered;

    public event Action OnLeftClickCb = delegate {  };

    public ButtonElement(Rect2D layout)
    {
        Layout = layout;

        normal = BorderBuilder.BuildBorderedTexture(layout.Extent.Width, layout.Extent.Height,
            new Rgba32(128, 128, 128, 128));
        hovered = BorderBuilder.BuildBorderedTexture(layout.Extent.Width, layout.Extent.Height,
            new Rgba32(128, 128, 128, byte.MaxValue));
        HasChanged = true;
    }

    private bool lastHoveredState;

    public override void Update(float deltaTime)
    {
        HasChanged = lastHoveredState != CursorHovering;
        lastHoveredState = CursorHovering;
    }

    public override void Draw(CommandBuffer copyBuffer, Texture target)
    {
        Texture toDraw = CursorHovering ? hovered : normal;

        Texture.CopyTo(copyBuffer, (toDraw, 0, 0, 0, 0, 0), (target, (uint)Layout.Offset.X, (uint)Layout.Offset.Y, 0, 0, 0),
            Layout.Extent.Width, Layout.Extent.Height, 1, 1);
    }

    public override void OnLeftClick()
    {
        OnLeftClickCb();
    }

    public override void Dispose()
    {
        base.Dispose();
        hovered.Dispose();
        normal.Dispose();
    }
}