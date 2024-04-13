using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Threading;
using DotNext.Diagnostics;
using MintyCore.Components.Common;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Input;
using MintyCore.Modding;
using MintyCore.Utils;
using MintyCore.Utils.Events;
using Serilog;
using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using static MintyCore.AvaloniaIntegration.AvaloniaUtils;
using GlfwKeyModifiers = Silk.NET.GLFW.KeyModifiers;
using AvaloniaKeyModifiers = Avalonia.Input.KeyModifiers;

namespace MintyCore.AvaloniaIntegration;

[Singleton<IAvaloniaController>(SingletonContextFlags.NoHeadless)]
internal class AvaloniaController(
    IUiPlatform uiPlatform,
    IModManager modManager,
    IVulkanEngine vulkanEngine,
    ITextureManager textureManager,
    IEventBus eventBus
) : IAvaloniaController
{
    private MintyCoreTopLevel? _topLevel;
    private Thread? _uiThread;
    private CancellationTokenSource? _uiThreadCts;

    private IKeyboardDevice? _keyboardDevice;

    private IKeyboardDevice KeyboardDevice => _keyboardDevice ??=
        AvaloniaLocator.Current.GetService<IKeyboardDevice>() ??
        throw new InvalidOperationException("No keyboard device found");

    private IMouseDevice? _mouseDevice;

    private IMouseDevice MouseDevice => _mouseDevice ??= AvaloniaLocator.Current.GetService<IMouseDevice>() ??
                                                         throw new InvalidOperationException("No mouse device found");

    public MintyCoreTopLevel TopLevel => _topLevel ?? throw new InvalidOperationException("Not initialized");

    private bool _isInitialized;

    private double _scaling = 1.0;

    public double Scaling
    {
        get => _scaling;
        set
        {
            _scaling = value;
            TopLevel.Impl.SetRenderScaling(value);
        }
    }


    public void SetupAndRun()
    {
        _uiThreadCts = new CancellationTokenSource();

        _uiThread = new Thread(SetupAndRunInternal)
        {
            IsBackground = true,
            Name = "Avalonia UI Thread"
        };
        _uiThread.Start();

        SpinWait.SpinUntil(() => _isInitialized);
    }


    public void Stop()
    {
        _uiThreadCts?.Cancel();
        _uiThread?.Join();

        _uiThreadCts?.Dispose();
        _uiThreadCts = null;

        _uiThread = null;
    }

    private void SetupAndRunInternal()
    {
        AppBuilder.Configure<App>()
            .UseMintyCore(uiPlatform, modManager, vulkanEngine, textureManager)
            .SetupWithoutStarting();


        var window = Engine.Window!;

        var locator = AvaloniaLocator.Current;
        if (locator.GetService<IPlatformGraphics>() is not VkPlatformGraphics platformGraphics)
        {
            Log.Error("No valid platform graphics found");
            return;
        }

        var topLevelImpl = new MintyCoreTopLevelImpl(platformGraphics, locator.GetRequiredService<IClipboard>(),
            uiPlatform.Compositor);
        topLevelImpl.SetRenderSize(
            new PixelSize(window.WindowInstance.FramebufferSize.X, window.WindowInstance.FramebufferSize.Y), Scaling);

        _topLevel = new MintyCoreTopLevel(topLevelImpl);
        _topLevel.Prepare();
        _topLevel.StartRendering();

        window.WindowInstance.FramebufferResize += OnWindowResized;

        _topLevel.Background = null;

        _isInitialized = true;
        Dispatcher.UIThread.MainLoop(_uiThreadCts!.Token);
        _isInitialized = false;

        // Cleanup. This is called after the cancellation token is cancelled
        //Must happen on the UI thread
        _topLevel?.Impl.Dispose();
        uiPlatform.Dispose();
        _topLevel?.Dispose();
    }

    public void TriggerScroll(float deltaX, float deltaY)
    {
        if(!_isInitialized) return;
        
        var inputEventArgs = new RawMouseWheelEventArgs(MouseDevice, (ulong)Stopwatch.GetTimestamp(),
            TopLevel.Impl.InputRoot, _mousePosition, new Vector(deltaX, deltaY), RawInputModifiers);

        TopLevel.Impl.InvokeInput(inputEventArgs);
    }

    private Point _mousePosition;

    public void TriggerCursorPos(float x, float y)
    {
        if(!_isInitialized) return;
        
        _mousePosition = new Point(x, y) / TopLevel.RenderScaling;

        var inputEventArgs = new RawPointerEventArgs(MouseDevice, (ulong)Stopwatch.GetTimestamp(),
            TopLevel.Impl.InputRoot, RawPointerEventType.Move, _mousePosition,
            RawInputModifiers);

        TopLevel.Impl.InvokeInput(inputEventArgs);
        
        TopLevel.Impl.InvokeLostFocus();
    }

    private RawInputModifiers _rawMouseModifiers = RawInputModifiers.None;
    private RawInputModifiers _rawKeyboardModifiers = RawInputModifiers.None;
    private RawInputModifiers RawInputModifiers => _rawMouseModifiers | _rawKeyboardModifiers;

    public void TriggerMouseButton(MouseButton button, InputAction action, GlfwKeyModifiers mods)
    {
        if(!_isInitialized) return;
        
        if (!TryGetRawPointerEventType(button, action, out var eventType)) return;

        _rawKeyboardModifiers = mods.ToAvalonia();

        switch (button)
        {
            case MouseButton.Left:
            {
                if (action == InputAction.Press)
                    _rawMouseModifiers |= RawInputModifiers.LeftMouseButton;
                if (action == InputAction.Release)
                    _rawMouseModifiers &= ~RawInputModifiers.LeftMouseButton;
                break;
            }
            case MouseButton.Right:
            {
                if (action == InputAction.Press)
                    _rawMouseModifiers |= RawInputModifiers.RightMouseButton;
                if (action == InputAction.Release)
                    _rawMouseModifiers &= ~RawInputModifiers.RightMouseButton;
                break;
            }
            case MouseButton.Middle:
            {
                if (action == InputAction.Press)
                    _rawMouseModifiers |= RawInputModifiers.MiddleMouseButton;
                if (action == InputAction.Release)
                    _rawMouseModifiers &= ~RawInputModifiers.MiddleMouseButton;
                break;
            }
        }


        var inputEventArgs = new RawPointerEventArgs(MouseDevice, (ulong)Stopwatch.GetTimestamp(),
            TopLevel.Impl.InputRoot, eventType, _mousePosition,
            RawInputModifiers);

        TopLevel.Impl.InvokeInput(inputEventArgs);
    }

    public void TriggerKey(Key physicalKey, InputAction action, GlfwKeyModifiers keyModifiers, string? localizedKeyRep)
    {
        if(!_isInitialized) return;
        
        if (action == InputAction.Repeat) return;

        _rawKeyboardModifiers = keyModifiers.ToAvalonia();

        var inputEventArgs = new RawKeyEventArgs(KeyboardDevice, (ulong)Stopwatch.GetTimestamp(),
            TopLevel.Impl.InputRoot, action.ToAvalonia(), physicalKey.ToLogicalAvaloniaKey(localizedKeyRep),
            RawInputModifiers, physicalKey.ToAvalonia(), localizedKeyRep);

        TopLevel.Impl.InvokeInput(inputEventArgs);
    }

    public void TriggerChar(char character)
    {
        if(!_isInitialized) return;
        
        var inputEventArgs = new RawTextInputEventArgs(KeyboardDevice, (ulong)Stopwatch.GetTimestamp(),
            TopLevel.Impl.InputRoot, character.ToString());

        TopLevel.Impl.InvokeInput(inputEventArgs);
    }

    public Texture Draw(Texture? texture)
    {
        return Dispatcher.UIThread.Invoke(() => DrawInternal(texture));
    }

    private Texture DrawInternal(Texture? texture)
    {
        TopLevel.Impl.OnDraw();
        uiPlatform.TriggerRender();
        var uiTexture = TopLevel.Impl.GetTexture();

        if (texture is null || texture.Width != uiTexture.Width || texture.Height != uiTexture.Height)
        {
            var textureDescription = TextureDescription.Texture2D(uiTexture.Width, uiTexture.Height, 1, 1,
                Format.R8G8B8A8Unorm,
                TextureUsage.Sampled);
            texture = textureManager.Create(ref textureDescription);
        }

        var cb = vulkanEngine.GetSingleTimeCommandBuffer();
        cb.CopyTexture(uiTexture, texture);
        vulkanEngine.ExecuteSingleTimeCommandBuffer(cb);

        return texture;
    }


    private void OnWindowResized(Vector2D<int> vector2D)
    {
        TopLevel.Impl.SetRenderSize(new PixelSize(vector2D.X, vector2D.Y));
    }
}