using System;
using System.Numerics;
using MintyCore.Identifications;
using MintyCore.Render;
using Silk.NET.Vulkan;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

public class TextBox : Element
{
    private Image<Rgba32> _image;
    private Texture _texture;
    private string _content;

    public TextBox(Vector2 center, string content = "") : base(CalculateLayout(center, content))
    {
        _image = new Image<Rgba32>((int)Layout.Extent.Width, (int)Layout.Extent.Height);

        TextureDescription description = TextureDescription.Texture2D(Layout.Extent.Width, Layout.Extent.Height, 1, 1,
            Format.R8G8B8A8Unorm, TextureUsage.STAGING);
        _texture = new Texture(ref description);
        _content = content;
        UpdateImage();
    }

    public TextBox(Offset2D offset, string content = "") : base(CalculateLayout(offset, content))
    {
        _image = new Image<Rgba32>((int)Layout.Extent.Width, (int)Layout.Extent.Height);

        TextureDescription description = TextureDescription.Texture2D(Layout.Extent.Width, Layout.Extent.Height, 1, 1,
            Format.R8G8B8A8Unorm, TextureUsage.STAGING);
        _texture = new Texture(ref description);
        _content = content;
        UpdateImage();
    }

    private static Rect2D CalculateLayout(Offset2D parentOffset, string content)
    {
        var size = TextMeasurer.MeasureBounds(content, new RendererOptions(FontHandler.GetFont(FontIDs.Akashi)));
        Extent2D extent = new((uint)size.Width,(uint) size.Height);
        return new(parentOffset, extent);
    }

    private static Rect2D CalculateLayout(Vector2 center, string content)
    {
        var size = TextMeasurer.MeasureBounds(content, new RendererOptions(FontHandler.GetFont(FontIDs.Akashi)));

        Offset2D offset = new((int)(center.X - (size.Width / 2f)), (int)(center.Y - size.Height));
        Extent2D extent = new((uint)size.Width, (uint)size.Height);
        
        return new (offset, extent);
    }

    private void UpdateImage()
    {
        _image.Mutate(x =>
        {
            x.DrawText(new(), _content, FontHandler.GetFont(FontIDs.Akashi), Color.White,
                new PointF(0, 0));
        });
        TextureHandler.CopyImageToTexture(new[] { _image }.AsSpan(), _texture, true);
    }

    public override void Draw(CommandBuffer copyBuffer, Texture target)
    {
        Texture.CopyTo(copyBuffer, (_texture, 0, 0, 0, 0, 0),
            (target, (uint)Layout.Offset.X, (uint)Layout.Offset.Y, 0, 0, 0),
            Layout.Extent.Width, Layout.Extent.Height, 1, 1);
    }

    public override void Dispose()
    {
        base.Dispose();
        _image.Dispose();
        _texture.Dispose();
    }
}