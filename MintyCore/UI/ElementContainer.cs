using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MintyCore.Render;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MintyCore.UI;

public class ElementContainer : Element
{
    private List<Element> _containingElements = new();

    private Image<Rgba32> _image;

    public ElementContainer(Layout layout) : base(layout)
    {
    }

    public override void Initialize()
    {
        _image = new Image<Rgba32>((int)PixelSize.X, (int) PixelSize.Y);
    }

    public override void Resize()
    {
        _image.Dispose();
        _image = new Image<Rgba32>((int)PixelSize.X, (int) PixelSize.Y);
        foreach (var element in _containingElements)
        {
            element.Resize();
        }
    }

    public void AddElement(Element element)
    {
        if (element.IsRootElement)
        {
            Logger.WriteLog("Root element can not be added as a child", LogImportance.EXCEPTION, "UI");
        }

        if (!MathHelper.Contains(new Layout(new Vector2(0), new Vector2(1)), element.Layout))
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
        element.Initialize();
    }

    public override Image<Rgba32> Image
    {
        get
        {
            foreach (var element in _containingElements)
            {
                CopyImage(_image, element.Image, PixelSize * element.Layout.Offset);
            }

            return _image;
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

        _image.Dispose();
    }
}