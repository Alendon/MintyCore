using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MintyCore.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

/// <summary>
/// Abstract base class for all Ui Elements
/// </summary>
public abstract class Element : IDisposable
{
    /// <summary>
    /// Did the Element changed last frame
    /// </summary>
    public bool HasChanged { get; protected set; }
        
    /// <summary>
    /// The parent of this Element
    /// </summary>
    public Element? Parent { get; set; }
        
    /// <summary>
    /// Whether or not this element is a root element
    /// </summary>
    public bool IsRootElement { get; init; }
        
    /// <summary>
    /// The layout off the element relative to the parent
    /// Values needs to be in Range 0f-1f
    /// <remarks>The (0,0) coordinate is the upper left corner</remarks>
    /// </summary>
    public Layout Layout { get; protected set; }

    /// <summary>
    /// The image representing this Ui Element
    /// </summary>
    public abstract Image<Rgba32> Image { get; }

    /// <summary>
    /// helper method to copy a image to another
    /// </summary>
    /// <param name="destination">Destination image to copy to</param>
    /// <param name="source">Source image to copy from</param>
    /// <param name="offset">Offset on the destination image</param>
    protected static void CopyImage(Image<Rgba32> destination, Image<Rgba32> source, Vector2 offset)
    {
        destination.Mutate(context => context.DrawImage(source, new Point((int)offset.X, (int)offset.Y), 1f));
    }
    
    /// <summary>
    /// Whether or not the cursor is hovering over the element
    /// </summary>
    public bool CursorHovering { get; set; }
    
    /// <summary>
    /// The cursor position relative to the element
    /// </summary>
    public Vector2 CursorPosition { get; set; }

    ///<summary/>
    public Element(Layout layout)
    {
        Layout = layout;
    }
    
    /// <summary>
    /// The absolute pixel size of the element
    /// </summary>
    public virtual Vector2 PixelSize
    {
        get
        {
            Logger.Assert(!IsRootElement, $"RootElements have to override {nameof(PixelSize)}", "UI");
            Logger.Assert(Parent != null, $"Cannot get pixel size of element as parent is null", "UI");
            return Parent!.PixelSize * Layout.Extent;
        }
    }

    /// <summary>
    /// Get the children of this element
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerable<Element> GetChildElements()
    {
        return Enumerable.Empty<Element>();
    }

    /// <summary>
    /// Update the element
    /// </summary>
    /// <param name="deltaTime">Time since last tick</param>
    public virtual void Update(float deltaTime)
    {
            
    }
    
    /// <summary>
    /// Initialize the element
    /// </summary>
    public abstract void Initialize();

    /// <summary>
    /// Resize the element
    /// </summary>
    public abstract void Resize();

    /// <summary>
    /// Get/set whether or not this component is active (will get updated)
    /// </summary>
    public virtual bool IsActive { get; set; }
    
    /// <summary>
    /// Triggered when the cursor enters the element
    /// </summary>
    public virtual void OnCursorEnter()
    {
            
    }

    /// <summary>
    /// Triggered when the cursor leaves the element
    /// </summary>
    public virtual void OnCursorLeave()
    {
            
    }

    /// <summary>
    /// A left click is performed (gets called even when cursor not inside of element)
    /// </summary>
    public virtual void OnLeftClick()
    {
            
    }

    /// <summary>
    /// A right click is performed (gets called even when cursor not inside of element)
    /// </summary>
    public virtual void OnRightClick()
    {
            
    }

    /// <summary>
    /// A scroll is performed (gets called even when cursor not inside of element)
    /// </summary>
    public virtual void OnScroll(Vector2 movement)
    {
            
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        
    }
}

/// <summary>
/// Layout of the element in relative coordinates
/// (0,0) is the upper left corner
/// </summary>
public readonly struct Layout
{
    /// <summary>
    /// The offset relative to the parent element
    /// Must be in Range of 0-1
    /// </summary>
    public readonly Vector2 Offset;
    /// <summary>
    /// The extent relative to the parent element
    /// Must be in Range of 0-1
    /// </summary>
    public readonly Vector2 Extent;

    /// <summary>
    /// Constructor for the layout
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="extent"></param>
    public Layout(Vector2 offset, Vector2 extent)
    {
        Offset = offset;
        Extent = extent;
    }
}