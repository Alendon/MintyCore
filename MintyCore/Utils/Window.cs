using System;
using System.Threading;
using JetBrains.Annotations;
using MintyCore.Input;
using Silk.NET.Core.Contexts;
using Silk.NET.GLFW;
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
                Title = "Techardry",
            }; //(100, 100, 960, 540, WindowState.Normal, "Techardry");

        Silk.NET.Windowing.Window.PrioritizeGlfw();
        WindowInstance = Silk.NET.Windowing.Window.Create(options);

        WindowInstance.Initialize();

        if (WindowInstance.Native?.Kind.HasFlag(NativeWindowFlags.Glfw) is not true ||
            WindowInstance.Native?.Glfw is null ||
            WindowInstance.Native?.Glfw == IntPtr.Zero)
            throw new MintyCoreException(
                $"Failed to create GLFW window instance. Window instance \"{WindowInstance.Native?.Kind.ToString()}\" is not supported");

        if (WindowInstance.VkSurface is null)
            throw new MintyCoreException("Vulkan surface was not created");

        _inputHandler.Setup(this);
    }

    /// <summary>
    ///     Interface representing the window
    /// </summary>
    public IWindow WindowInstance { get; }

    private unsafe WindowHandle* WindowHandle =>
        (WindowHandle*)(WindowInstance.Native?.Glfw ?? throw new MintyCoreException("WindowHandle is null"));

    /// <summary>
    ///     The size of the window
    /// </summary>
    public Vector2D<int> Size => WindowInstance.Size;

    /// <summary>
    ///     The framebuffer size of the window
    /// </summary>
    public Vector2D<int> FramebufferSize => WindowInstance.FramebufferSize;

    private bool _mouseLocked;
    private bool _updateMouseLock;

    /// <summary>
    ///     Get or set the mouse locked state
    /// </summary>
    public bool MouseLocked
    {
        get => _mouseLocked;
        set
        {
            var changed = _mouseLocked != value;
            _mouseLocked = value;

            if (!changed) return;

            if (Thread.CurrentThread != Engine.MainThread) _updateMouseLock = true;
            else UpdateMouseLock();
        }
    }

    private unsafe void UpdateMouseLock()
    {
        if (Thread.CurrentThread != Engine.MainThread)
            throw new MintyCoreException("UpdateMouseLock must be called from the main thread");

        var api = Glfw.GetApi();

        api.SetInputMode(WindowHandle, CursorStateAttribute.Cursor,
            MouseLocked ? CursorModeValue.CursorDisabled : CursorModeValue.CursorNormal);

        _updateMouseLock = false;
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
        
        if(_updateMouseLock) UpdateMouseLock();
    }
}