using System.Collections.Generic;
using System.Linq;
using MintyCore.Render;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

public class ElementContainer : Element
{
    private List<Element> _containingElements = new();

    private Texture _texture;

    public ElementContainer(Rect2D layout)
    {
        Layout = layout;

        TextureDescription description = TextureDescription.Texture2D(layout.Extent.Width, layout.Extent.Height, 1, 1,
            Format.R8G8B8A8Unorm, TextureUsage.STAGING);
        _texture = new Texture(ref description);
    }

    protected void Resize(Extent2D newSize)
    {
        Layout = new Rect2D(Layout.Offset, newSize);
        
        TextureDescription description = TextureDescription.Texture2D(newSize.Width, newSize.Height, 1, 1,
            Format.R8G8B8A8Unorm, TextureUsage.STAGING);
        _texture = new Texture(ref description);
    }

    public void AddElement(Element element)
    {
        if (element.IsRootElement)
        {
            Logger.WriteLog("Root element can not be added as a child", LogImportance.EXCEPTION, "UI");
        }

        if (!MathHelper.Contains(Layout, element.Layout))
        {
            Logger.WriteLog($"Element to add is not inside parent bounds", LogImportance.ERROR, "UI");
            return;
        }

        foreach (var childElement in _containingElements)
        {
            if (!MathHelper.Overlaps(childElement.Layout, element.Layout)) continue;
            Logger.WriteLog("Element to add overlaps with existing element", LogImportance.ERROR, "UI");
            return;
        }

        _containingElements.Add(element);
        element.Parent = this;
    }

    public override void Draw(CommandBuffer copyBuffer, Texture target)
    {
        HasChanged = false;

        foreach (var element in _containingElements)
        {
            HasChanged = true;
            element.Draw(copyBuffer, _texture);
        }

        if (HasChanged)
        {
            Texture.CopyTo(copyBuffer, (_texture, 0, 0, 0, 0, 0),
                (target, (uint)Layout.Offset.X, (uint)Layout.Offset.Y, 0, 0, 0),
                Layout.Extent.Width, Layout.Extent.Height, 1, 1);
        }
    }

    public override IEnumerable<Element> GetChildElements()
    {
        return _containingElements;
    }

    public override void Dispose()
    {
        base.Dispose();
        foreach (var element in _containingElements)
        {
            element.Dispose();
        }
        _texture.Dispose();
    }
}