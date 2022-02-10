using System.Collections.Generic;
using System.Numerics;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MintyCore.UI;

/// <summary>
/// A generic element which can contain multiple child elements
/// </summary>
public class ElementContainer : Element
{
    private readonly List<Element> _containingElements = new();

    protected Image<Rgba32> _image;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="layout"></param>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public ElementContainer(RectangleF layout) : base(layout)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        _image = new Image<Rgba32>((int)PixelSize.Width, (int) PixelSize.Height);
    }

    /// <inheritdoc />
    public override void Resize()
    {
        _image.Dispose();
        if (PixelSize.Width == 0 || PixelSize.Height == 0) return;
        _image = new Image<Rgba32>((int)PixelSize.Width, (int) PixelSize.Height);
        foreach (var element in _containingElements)
        {
            element.Resize();
        }
    }

    /// <summary>
    /// Add a new child element
    /// </summary>
    /// <param name="element">Element to add as a child</param>
    public void AddElement(Element element)
    {
        if (element.IsRootElement)
        {
            Logger.WriteLog("Root element can not be added as a child", LogImportance.EXCEPTION, "UI");
        }

        if ( !Layout.Contains(element.Layout))
        {
            Logger.WriteLog($"Element to add is not inside parent bounds", LogImportance.ERROR, "UI");
            return;
        }

        foreach (var childElement in _containingElements)
        {
            if ( !element.Layout.IntersectsWith(childElement.Layout)) continue;
            Logger.WriteLog("Element to add overlaps with existing element", LogImportance.ERROR, "UI");
            return;
        }

        _containingElements.Add(element);
        element.Parent = this;
        element.Initialize();
    }

    /// <inheritdoc />
    public override Image<Rgba32> Image
    {
        get
        {
            foreach (var element in _containingElements)
            {
                CopyImage(_image, element.Image,  new((int)(PixelSize.Width * element.Layout.X), (int)(PixelSize.Height * element.Layout.Y)));
            }

            return _image;
        }
    }

    /// <inheritdoc />
    public override IEnumerable<Element> GetChildElements()
    {
        return _containingElements;
    }

    /// <inheritdoc />
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