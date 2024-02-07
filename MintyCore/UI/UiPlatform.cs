using System.Drawing;
using MintyCore.Utils;
using Myra.Graphics2D.UI;
using Myra.Platform;
using Silk.NET.Input;
using Silk.NET.Maths;

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


    public UiPlatform(IUiRenderer renderer, IInputHandler inputHandler)
    {
        Renderer = renderer;
        InputHandler = inputHandler;
    }
    
    /// <inheritdoc />
    public MouseInfo GetMouseInfo()
    {
        return new MouseInfo
        {
            Position = new Point((int) InputHandler.MousePosition.X, (int) InputHandler.MousePosition.Y),
            Wheel = (int) InputHandler.ScrollWheelDelta.Y,
            IsLeftButtonDown = InputHandler.GetMouseDown(MouseButton.Left),
            IsMiddleButtonDown = InputHandler.GetMouseDown(MouseButton.Middle),
            IsRightButtonDown = InputHandler.GetMouseDown(MouseButton.Right)
        };
    }

    /// <inheritdoc />
    public void SetKeysDown(bool[] keys)
    {
        //TODO implement
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