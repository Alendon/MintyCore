using System;
using System.Collections.Generic;
using System.Numerics;
using MintyCore.Utils;
using Silk.NET.Input;
using SixLabors.ImageSharp;

namespace MintyCore.UI;

/// <summary>
///     Class to handle the user interface
/// </summary>
public static class UiHandler
{
    private static readonly Dictionary<Identification, Identification> _uiRootElementCreators = new();
    private static readonly Dictionary<Identification, Element> _uiRootElements = new();
    private static readonly Dictionary<Identification, Func<Element>> _elementPrefabs = new();

    private static bool _lastLeftMouseState;
    private static bool _lastRightMouseState;

    private static bool _currentLeftMouseState;
    private static bool _currentRightMouseState;

    /// <summary>
    ///     Add a root element (an element which gets automatically updated)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="element"></param>
    public static void AddRootElement(Identification id, Identification element)
    {
        _uiRootElementCreators.Add(id, element);
    }

    internal static void AddElementPrefab(Identification id, Func<Element> prefab)
    {
        _elementPrefabs.Add(id, prefab);
    }

    internal static void SetElementPrefab(Identification prefabId, Func<Element> prefabCreator)
    {
        _elementPrefabs.Remove(prefabId);
        AddElementPrefab(prefabId, prefabCreator);
    }

    internal static void SetRootElement(Identification elementId, Identification rootElement)
    {
        _uiRootElements.Remove(elementId);
        AddRootElement(elementId, rootElement);
    }

    /// <summary>
    ///     Get a root element
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static Element GetRootElement(Identification id)
    {
        return _uiRootElements[id];
    }

    /// <summary>
    ///     Create a new element
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static Element CreateElement(Identification id)
    {
        return _elementPrefabs[id]();
    }


    internal static void Update()
    {
        _lastLeftMouseState = _currentLeftMouseState;
        _lastRightMouseState = _currentRightMouseState;
        _currentLeftMouseState = InputHandler.GetMouseDown(MouseButton.Left);
        _currentRightMouseState = InputHandler.GetMouseDown(MouseButton.Right);

        try
        {
            foreach (var element in _uiRootElements.Values) UpdateElement(element);
        }
        catch (InvalidOperationException)
        {
            //Ignore. Happens when Main Menu is updated, a game starts and end. At the end the root elements get cleared. The collection invalidated.
            //TODO fix this
        }
    }

    /// <summary>
    ///     Update a specific element
    /// </summary>
    /// <param name="element">Element to update</param>
    /// <param name="absoluteOffset">The absolute offset from (0,0)</param>
    /// <param name="updateChildren">Whether or not the children should be updated</param>
    public static void UpdateElement(Element element, PointF absoluteOffset = default, bool updateChildren = true)
    {
        if (!element.IsActive) return;

        var cursorPos = GetUiCursorPosition();

        var absoluteLayout = new RectangleF(absoluteOffset, new SizeF(element.PixelSize));

        if (absoluteLayout.Contains(cursorPos))
        {
            if (!element.CursorHovering)
            {
                element.CursorHovering = true;
                element.OnCursorEnter();
            }

            element.CursorPosition = new Vector2(cursorPos.X - absoluteOffset.X - element.Layout.X,
                cursorPos.Y - absoluteOffset.Y - element.Layout.Y);
        }
        else
        {
            if (element.CursorHovering)
            {
                element.CursorHovering = false;
                element.OnCursorLeave();
            }
        }

        if (!_lastLeftMouseState && _currentLeftMouseState) element.OnLeftClick();

        if (!_lastRightMouseState && _currentRightMouseState) element.OnRightClick();

        if (InputHandler.ScrollWheelDelta != Vector2.Zero) element.OnScroll(InputHandler.ScrollWheelDelta);

        element.Update(Engine.DeltaTime);
        if (!updateChildren) return;
        foreach (var childElement in element.GetChildElements())
        {
            var childOffset = absoluteOffset + new PointF(element.PixelSize.Width * childElement.Layout.X,
                element.PixelSize.Height * childElement.Layout.Y);
            UpdateElement(childElement, childOffset);
        }
    }

    private static Vector2 GetUiCursorPosition()
    {
        return new Vector2(InputHandler.MousePosition.X,
            Engine.Window!.Size.Y - InputHandler.MousePosition.Y);
    }

    internal static void Clear()
    {
        foreach (var element in _uiRootElements.Values) element.Dispose();

        _uiRootElementCreators.Clear();
        _uiRootElements.Clear();
        _elementPrefabs.Clear();
    }

    internal static void RemoveElement(Identification objectId)
    {
        _elementPrefabs.Remove(objectId);
        if (_uiRootElements.Remove(objectId, out var element)) element.Dispose();
        _uiRootElementCreators.Remove(objectId);
    }

    internal static void CreateRootElements()
    {
        foreach (var (elementId, creatorId) in _uiRootElementCreators)
        {
            if (_uiRootElements.ContainsKey(elementId)) continue;
            _uiRootElements.Add(elementId, CreateElement(creatorId));
        }
    }
}