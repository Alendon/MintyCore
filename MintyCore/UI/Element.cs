using System;
using System.Collections.Generic;
using System.Numerics;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
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
        
    public bool IsRootElement { get;  init; }
        
    /// <summary>
    /// The layout off the element relative to the parent
    /// Values needs to be in Range 0f-1f
    /// <remarks>The (0,0) coordinate is the upper left corner</remarks>
    /// </summary>
    public Layout Layout { get; protected set; }

    public abstract Image<Rgba32> Image { get; }

    protected static void CopyImage(Image<Rgba32> destination, Image<Rgba32> source, Vector2 offset)
    {
        destination.Mutate(context => context.DrawImage(source, new Point((int)offset.X, (int)offset.Y), 1f));
    }
    
    public bool CursorHovering { get; set; }
    
    public Vector2 CursorPosition { get; set; }

    public Element(Layout layout)
    {
        Layout = layout;
    }

    public virtual Vector2 PixelSize
    {
        get
        {
            Logger.Assert(!IsRootElement, $"RootElements have to override {nameof(PixelSize)}", "UI");
            Logger.Assert(Parent != null, $"Cannot get pixel size of element as parent is null", "UI");
            return Parent!.PixelSize * Layout.Extent;
        }
    }

    public virtual IEnumerable<Element> GetChildElements()
    {
        return Array.Empty<Element>();
    }

    public virtual void Update(float deltaTime)
    {
            
    }

    public abstract void Initialize();

    public abstract void Resize();

    public virtual void Activate()
    {
            
    }

    public virtual void Deactivate()
    {
            
    }

    public virtual void OnCursorEnter()
    {
            
    }

    public virtual void OnCursorLeave()
    {
            
    }

    public virtual void OnLeftClick()
    {
            
    }

    public virtual void OnRightClick()
    {
            
    }

    public virtual void OnScroll(Vector2 movement)
    {
            
    }

    public virtual void Dispose()
    {
        
    }
}

public readonly struct Layout
{
    public readonly Vector2 Offset;
    public readonly Vector2 Extent;

    public Layout(Vector2 offset, Vector2 extent)
    {
        Offset = offset;
        Extent = extent;
    }
}