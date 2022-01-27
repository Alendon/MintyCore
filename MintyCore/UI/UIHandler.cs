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


    public static void UpdateElement(Element element, Offset2D parentOffset = default, bool updateChildren = true)
    {
        if (MathHelper.InRectangle(new()
            {
                Extent = element.Layout.Extent,
                Offset = new Offset2D { X = parentOffset.X + element.Layout.Offset.X, Y = parentOffset.Y + element.Layout.Offset.Y }
            }, InputHandler.LastMousePos))
        {
            if (!element.CursorHovering)
            {
                element.CursorHovering = true;
                element.OnCursorEnter();
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
        }
        else
        {
            if (element.CursorHovering)
            {
                element.CursorHovering = false;
                element.OnCursorLeave();
            }
        }

        element.Update(Engine.DeltaTime);
        if (!updateChildren) return;
        foreach (var childElement in element.GetChildElements())
        {
            UpdateElement(childElement,
                new Offset2D { X = parentOffset.X + element.Layout.Offset.X, Y = parentOffset.Y + element.Layout.Offset.Y });
        }
    }
}