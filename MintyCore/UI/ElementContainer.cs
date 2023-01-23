using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MintyCore.UI;

/// <summary>
///     A generic element which can contain multiple child elements
/// </summary>
public class ElementContainer : Element
{
    private readonly List<Element> _containingElements = new();

    /// <summary />
    protected Image<Rgba32>? CombinedImage;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="layout"></param>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public ElementContainer(RectangleF layout) : base(layout)
    {
    }

    /// <inheritdoc />
    public override Image<Rgba32>? Image
    {
        get
        {
            if (CombinedImage is null) return null;

            foreach (var element in _containingElements)
            {
                if (element.Image is null) continue;

                CopyImage(CombinedImage, element.Image,
                    new Point((int) (PixelSize.Width * element.Layout.X), (int) (PixelSize.Height * element.Layout.Y)));
            }

            return CombinedImage;
        }
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        CombinedImage = new Image<Rgba32>((int) PixelSize.Width, (int) PixelSize.Height);
    }

    /// <inheritdoc />
    public override void Resize()
    {
        CombinedImage?.Dispose();
        if (PixelSize.Width == 0 || PixelSize.Height == 0) return;
        CombinedImage = new Image<Rgba32>((int) PixelSize.Width, (int) PixelSize.Height);
        foreach (var element in _containingElements) element.Resize();
    }

    /// <summary>
    ///     Add a new child element
    /// </summary>
    /// <param name="element">Element to add as a child</param>
    public void AddElement(Element element)
    {
        if (element.IsRootElement)
            Logger.WriteLog("Root element can not be added as a child", LogImportance.Exception, "UI");

        if (!Layout.Contains(element.Layout))
        {
            Logger.WriteLog("Element to add is not inside parent bounds", LogImportance.Error, "UI");
            return;
        }

        if (_containingElements.Any(childElement => element.Layout.IntersectsWith(childElement.Layout)))
        {
            Logger.WriteLog("Element to add overlaps with existing element", LogImportance.Error, "UI");
            return;
        }

        _containingElements.Add(element);
        element.Parent = this;
        element.Initialize();
    }

    /// <inheritdoc />
    public override IEnumerable<Element> GetChildElements()
    {
        return _containingElements;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        base.Dispose();
        foreach (var element in _containingElements) element.Dispose();

        CombinedImage?.Dispose();
    }
}