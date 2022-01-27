using MintyCore.Render;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

public class TextureElement : Element
{
    public Texture Texture {get; private set; }


    public override void Draw(CommandBuffer copyBuffer, Texture target)
    {
        Texture.CopyTo(copyBuffer, (Texture, 0, 0, 0, 1, 1), (target, (uint)Layout.Offset.X, (uint)Layout.Offset.Y, 0, 1, 1),
            Layout.Extent.Width, Layout.Extent.Height, 1, 1);
    }
}