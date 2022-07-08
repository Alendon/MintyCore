using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using MintyCore.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

/// <summary>
///     Abstract base class for all Ui Elements
/// </summary>
[PublicAPI]
public abstract class Element : IDisposable
{
    /// <summary />
    protected Element(RectangleF layout)
    {
        Layout = layout;
    }

    /// <summary>
    ///     Did the Element changed last frame
    /// </summary>
    public bool HasChanged { get; protected set; }

    /// <summary>
    ///     The parent of this Element
    /// </summary>
    public Element? Parent { get; set; }

    /// <summary>
    ///     Whether or not this element is a root element
    /// </summary>
    public bool IsRootElement { get; init; }

    /// <summary>
    ///     The layout off the element relative to the parent
    ///     Values needs to be in Range 0f-1f
    ///     <remarks>The (0,0) coordinate is the upper left corner</remarks>
    /// </summary>
    public RectangleF Layout { get; }

    /// <summary>
    ///     The image representing this Ui Element
    /// </summary>
    public abstract Image<Rgba32>? Image { get; }

    /// <summary>
    ///     Whether or not the cursor is hovering over the element
    /// </summary>
    public bool CursorHovering { get; set; }

    /// <summary>
    ///     The cursor position relative to the element
    /// </summary>
    public Vector2 CursorPosition { get; set; }

    /// <summary>
    ///     The absolute pixel size of the element
    /// </summary>
    public virtual SizeF PixelSize
    {
        get
        {
            Logger.AssertAndThrow(!IsRootElement, $"RootElements have to override {nameof(PixelSize)}", "UI");
            Logger.AssertAndThrow(Parent != null, "Cannot get pixel size of element as parent is null", "UI");
            return new SizeF(Parent!.PixelSize.Width * Layout.Width, Parent!.PixelSize.Height * Layout.Height);
        }
    }

    /// <summary>
    ///     Get/set whether or not this component is active (will get updated)
    /// </summary>
    public virtual bool IsActive { get; set; }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     helper method to copy a image to another
    /// </summary>
    /// <param name="destination">Destination image to copy to</param>
    /// <param name="source">Source image to copy from</param>
    /// <param name="location">Location on the destination to draw to</param>
    protected static void CopyImage(Image<Rgba32> destination, Image<Rgba32> source, Point location)
    {
        destination.Mutate(context => context.DrawImage(source, location, 1f));
    }

    /// <summary>
    ///     Get the children of this element
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerable<Element> GetChildElements()
    {
        return Enumerable.Empty<Element>();
    }

    /// <summary>
    ///     Update the element
    /// </summary>
    /// <param name="deltaTime">Time since last tick</param>
    public virtual void Update(float deltaTime)
    {
    }

    /// <summary>
    ///     Initialize the element
    /// </summary>
    public abstract void Initialize();

    /// <summary>
    ///     Resize the element
    /// </summary>
    public abstract void Resize();

    /// <summary>
    ///     Triggered when the cursor enters the element
    /// </summary>
    public virtual void OnCursorEnter()
    {
    }

    /// <summary>
    ///     Triggered when the cursor leaves the element
    /// </summary>
    public virtual void OnCursorLeave()
    {
    }

    /// <summary>
    ///     A left click is performed (gets called even when cursor not inside of element)
    /// </summary>
    public virtual void OnLeftClick()
    {
    }

    /// <summary>
    ///     A right click is performed (gets called even when cursor not inside of element)
    /// </summary>
    public virtual void OnRightClick()
    {
    }

    /// <summary>
    ///     A scroll is performed (gets called even when cursor not inside of element)
    /// </summary>
    public virtual void OnScroll(Vector2 movement)
    {
    }
}