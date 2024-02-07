using System;
using System.Collections.Generic;
using System.Drawing;
using MintyCore.Utils;
using Myra.Graphics2D.UI;
using Myra.Platform;
using Silk.NET.Input;
using Silk.NET.Maths;
using MyraKey = Myra.Platform.Keys;
using SilkKey = Silk.NET.Input.Key;

namespace MintyCore.UI;

[Singleton<IUiPlatform>(SingletonContextFlags.NoHeadless)]
internal class UiPlatform : IUiPlatform
{
    /// <inheritdoc />
    public Point ViewSize { get; internal set; }

    /// <inheritdoc />
    public IMyraRenderer Renderer { get; }

    /// <inheritdoc />
    public void Resize(Vector2D<int> newSize)
    {
        ViewSize = new Point(newSize.X, newSize.Y);
    }

    private IInputHandler InputHandler { get; }

    private readonly Dictionary<MyraKey, SilkKey> _keyMap;

    public UiPlatform(IUiRenderer renderer, IInputHandler inputHandler)
    {
        Renderer = renderer;
        InputHandler = inputHandler;

        _keyMap = new Dictionary<MyraKey, SilkKey>();

        foreach (MyraKey myraKey in Enum.GetValues(typeof(MyraKey)))
        {
            var keyName = Enum.GetName(typeof(MyraKey), myraKey);
            if (Enum.TryParse(keyName, out SilkKey silkKey))
            {
                _keyMap[myraKey] = silkKey;
            }
        }
        
        _keyMap[MyraKey.Back] = SilkKey.Backspace;
        _keyMap[MyraKey.LeftShift] = SilkKey.ShiftLeft;
        _keyMap[MyraKey.RightShift] = SilkKey.ShiftRight;
    }

    /// <inheritdoc />
    public MouseInfo GetMouseInfo()
    {
        return new MouseInfo
        {
            Position = new Point((int)InputHandler.MousePosition.X, (int)InputHandler.MousePosition.Y),
            Wheel = (int)InputHandler.ScrollWheelDelta.Y,
            IsLeftButtonDown = InputHandler.GetMouseDown(MouseButton.Left),
            IsMiddleButtonDown = InputHandler.GetMouseDown(MouseButton.Middle),
            IsRightButtonDown = InputHandler.GetMouseDown(MouseButton.Right)
        };
    }

    /// <inheritdoc />
    public void SetKeysDown(bool[] keys)
    {
        for (var i = 0; i < keys.Length; i++)
        {
            var myraKey = (MyraKey)i;
            if (_keyMap.TryGetValue(myraKey, out var silkKey))
            {
                keys[i] = InputHandler.GetKeyDown(silkKey);
            }
        }
    }

    /// <inheritdoc />
    public void SetMouseCursorType(MouseCursorType mouseCursorType)
    {
        //Multiple cursors are currently not supported
    }

    /// <inheritdoc />
    public TouchCollection GetTouchState()
    {
        //There is no touch support in MintyCore
        return TouchCollection.Empty;
    }
}