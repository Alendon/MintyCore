using System.Numerics;
using JetBrains.Annotations;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace MintyCore.Utils;

/// <summary>
///     Class to manage <see cref="Silk.NET.Windowing.IWindow" />
/// </summary>
[PublicAPI]
public class Window
{
    private IInputHandler _inputHandler;
    
    /// <summary>
    ///     Create a new window
    /// </summary>
    public Window(IInputHandler inputHandler)
    {
        _inputHandler = inputHandler;
        
        var options =
            new WindowOptions(ViewOptions.DefaultVulkan)
            {
                Size = new Vector2D<int>(960, 540),
                Title = "Techardry"
            }; //(100, 100, 960, 540, WindowState.Normal, "Techardry");

        WindowInstance = Silk.NET.Windowing.Window.Create(options);

        WindowInstance.Initialize();

        if (WindowInstance.VkSurface is null)
            throw new MintyCoreException("Vulkan surface was not created");

        var inputContext = WindowInstance.CreateInput();
        Mouse = inputContext.Mice[0];
        Keyboard = inputContext.Keyboards[0];
        _inputHandler.Setup(Mouse, Keyboard);
    }

    /// <summary>
    ///     Interface representing the connected keyboard
    /// </summary>
    public IKeyboard Keyboard { get; }

    /// <summary>
    ///     Interface representing the connected mouse
    /// </summary>
    public IMouse Mouse { get; }

    /// <summary>
    ///     Interface representing the window
    /// </summary>
    public IWindow WindowInstance { get; }

    /// <summary>
    ///     The size of the window
    /// </summary>
    public Vector2D<int> Size => WindowInstance.Size;

    /// <summary>
    ///     The framebuffer size of the window
    /// </summary>
    public Vector2D<int> FramebufferSize => WindowInstance.FramebufferSize;

    /// <summary>
    ///     Get or set the mouse locked state
    /// </summary>
    public bool MouseLocked
    {
        get => Mouse.Cursor.CursorMode == CursorMode.Disabled;
        set => Mouse.Cursor.CursorMode = value ? CursorMode.Disabled : CursorMode.Normal;
    }

    /// <summary>
    ///     Check if the window exists
    /// </summary>
    public bool Exists => !WindowInstance.IsClosing;

    /// <summary>
    ///     Process all window events
    /// </summary>
    public void DoEvents(float deltaTime)
    {
        //TODO FUTURE: This method blocks while the window gets resized or moved. Fix this by eg implementing a custom event method
        WindowInstance.DoEvents();
        _inputHandler.Update(deltaTime);

        var mousePos = Mouse.Position;
        if (Mouse.Cursor.CursorMode == CursorMode.Hidden)
        {
            var center = new Vector2(WindowInstance.Size.X / 2f, WindowInstance.Size.Y / 2f);
            _inputHandler.MouseDelta = mousePos - center;
            Mouse.Position = center;
        }
        else
        {
            _inputHandler.MouseDelta = mousePos - _inputHandler.MousePosition;
            _inputHandler.MousePosition = mousePos;
        }
    }
}