using System;
using System.Collections.Generic;
using System.Numerics;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using Silk.NET.Input;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

public static class UIHandler
{
    private static Dictionary<Identification, Element> _uiRootElements = new();
    private static Dictionary<Identification, Func<Element>> _elementPrefabs = new();


    internal static void AddRootElement(Identification id, Element element)
    {
        _uiRootElements.Add(id, element);
    }

    internal static void AddElementPrefab(Identification id, Func<Element> prefab)
    {
        _elementPrefabs.Add(id, prefab);
    }

    public static Element GetRootElement(Identification id)
    {
        return _uiRootElements[id];
    }

    public static Element CreateElement(Identification id)
    {
        return _elementPrefabs[id]();
    }

    private static bool _lastLeftMouseState;
    private static bool _lastRightMouseState;

    private static bool _currentLeftMouseState;
    private static bool _currentRightMouseState;

    public static void Update()
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


    public static void UpdateElement(Element element, Vector2 absoluteOffset = default, bool updateChildren = true)
    {
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

        if (InputHandler.ScrollWhellDelta != Vector2.Zero)
        {
            element.OnScroll(InputHandler.ScrollWhellDelta);
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