using System;
using System.Collections.Generic;
using System.Numerics;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using Silk.NET.Input;

namespace MintyCore.UI;

/// <summary>
/// Class to handle the user interface
/// </summary>
public static class UiHandler
{
    private static Dictionary<Identification, Element> _uiRootElements = new();
    private static Dictionary<Identification, Func<Element>> _elementPrefabs = new();

    /// <summary>
    /// Add a root element (an element which gets automatically updated)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="element"></param>
    public static void AddRootElement(Identification id, Element element)
    {
        _uiRootElements.Add(id, element);
    }

    internal static void AddElementPrefab(Identification id, Func<Element> prefab)
    {
        _elementPrefabs.Add(id, prefab);
    }
    
    /// <summary>
    /// Get a root element
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static Element GetRootElement(Identification id)
    {
        return _uiRootElements[id];
    }
    
    /// <summary>
    /// Create a new element
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static Element CreateElement(Identification id)
    {
        return _elementPrefabs[id]();
    }

    private static bool _lastLeftMouseState;
    private static bool _lastRightMouseState;

    private static bool _currentLeftMouseState;
    private static bool _currentRightMouseState;

    
    internal static void Update()
    {
        _lastLeftMouseState = _currentLeftMouseState;
        _lastRightMouseState = _currentRightMouseState;
        _currentLeftMouseState = InputHandler.GetMouseDown(MouseButton.Left);
        _currentRightMouseState = InputHandler.GetMouseDown(MouseButton.Right);

        foreach (var element in _uiRootElements.Values)
        {
            UpdateElement(element);
        }
    }

    /// <summary>
    /// Update a specific element
    /// </summary>
    /// <param name="element">Element to update</param>
    /// <param name="absoluteOffset">The absolute offset from (0,0)</param>
    /// <param name="updateChildren">Whether or not the children should be updated</param>
    public static void UpdateElement(Element element, Vector2 absoluteOffset = default, bool updateChildren = true)
    {
        if (!element.IsActive) return;
        
        var cursorPos = GetUiCursorPosition();

        var absoluteLayout = new Layout(absoluteOffset, element.PixelSize);
        
        if (MathHelper.InRectangle(absoluteLayout, cursorPos))
        {
            if (!element.CursorHovering)
            {
                element.CursorHovering = true;
                element.OnCursorEnter();
            }

            element.CursorPosition = new Vector2(cursorPos.X - absoluteOffset.X - element.Layout.Offset.X ,
                cursorPos.Y - absoluteOffset.Y  - element.Layout.Offset.Y);
        }
        else
        {
            if (element.CursorHovering)
            {
                element.CursorHovering = false;
                element.OnCursorLeave();
            }
        }

        if (!_lastLeftMouseState && _currentLeftMouseState)
        {
            element.OnLeftClick();
        }

        if (!_lastRightMouseState && _currentRightMouseState)
        {
            element.OnRightClick();
        }

        if (InputHandler.ScrollWheelDelta != Vector2.Zero)
        {
            element.OnScroll(InputHandler.ScrollWheelDelta);
        }

        element.Update(Engine.DeltaTime);
        if (!updateChildren) return;
        foreach (var childElement in element.GetChildElements())
        {
            var childOffset = absoluteOffset + element.PixelSize * childElement.Layout.Offset;
            UpdateElement(childElement,childOffset);
        }
    }

    private static Vector2 GetUiCursorPosition( )
    {
        return new Vector2(InputHandler.MousePosition.X,
            Engine.Window!.Size.Y - InputHandler.MousePosition.Y);
    }
}