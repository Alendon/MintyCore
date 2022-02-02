using System;
using System.Collections.Generic;
using System.Numerics;
using MintyCore.Render;
using Silk.NET.Vulkan;

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
    /// The Layout of this Element
    /// <see cref="Rect2D.Extent"/> equals to the size of the Element
    /// <see cref="Rect2D.Offset"/> equals to the offset of the parent Element
    /// </summary>
    public Rect2D Layout { get; protected set; }

    public abstract void Draw(CommandBuffer copyBuffer, Texture target);
    
    public bool CursorHovering { get; set; }

    public Element(Rect2D layout)
    {
        Layout = layout;
    }

    public virtual IEnumerable<Element> GetChildElements()
    {
        return Array.Empty<Element>();
    }

    public virtual void Update(float deltaTime)
    {
            
    }

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