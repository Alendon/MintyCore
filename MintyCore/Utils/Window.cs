using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace MintyCore.Utils;

/// <summary>
///     Class to manage <see cref="Sdl2Window" />
/// </summary>
public class Window
{
    private readonly IKeyboard _keyboard;
    private readonly IMouse _mouse;
    private readonly IWindow _window;

    /// <summary>
    ///     Create a new window
    /// </summary>
    public Window()
    {
        var options =
            new WindowOptions(ViewOptions.DefaultVulkan); //(100, 100, 960, 540, WindowState.Normal, "Techardry");
        options.Size = new Vector2D<int>(960, 540);
        options.Title = "Techardry";

        _window = Silk.NET.Windowing.Window.Create(options);

        _window.Initialize();

        if (_window.VkSurface is null) throw new MintyCoreException("Vulkan surface was not created");

        var inputContext = _window.CreateInput();
        _mouse = inputContext.Mice[0];
        _keyboard = inputContext.Keyboards[0];
        InputHandler.Setup(_mouse, _keyboard);
    }


    public bool MouseLocked
    {
        get => _mouse.Cursor.CursorMode == CursorMode.Hidden;
        set => _mouse.Cursor.CursorMode = value ? CursorMode.Hidden : CursorMode.Normal;
    }

    /// <summary>
    ///     Check if the window exists
    /// </summary>
    public bool Exists => !_window.IsClosing;

    internal void DoEvents()
    {
        _window.DoEvents();
        InputHandler.Update();
        if (_mouse.Cursor.CursorMode == CursorMode.Hidden)
        {
            _mouse.Position = new Vector2(_window.Size.X / 2f, _window.Size.Y / 2f);
            InputHandler.LastMousePos = Vector2.Zero;
        }
    }

    internal IWindow GetWindow()
    {
        return _window;
    }
}