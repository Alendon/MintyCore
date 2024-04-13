using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using MintyCore.Graphics.VulkanObjects;
using Serilog;

namespace MintyCore.AvaloniaIntegration;

/// <inheritdoc />
public class MintyCoreTopLevelImpl : ITopLevelImpl
{
    private readonly VkPlatformGraphics _platformGraphics;

    private PixelSize _renderSize;
    private VkSkiaSurface? _surfaceInternal;

    private VkSkiaSurface? Surface
    {
        get
        {
            if (!Dispatcher.UIThread.CheckAccess())
                throw new InvalidOperationException("This property can only be accessed on the UI thread");

            return _surfaceInternal;
        }
        set
        {
            if (!Dispatcher.UIThread.CheckAccess())
                throw new InvalidOperationException("This property can only be accessed on the UI thread");

            _surfaceInternal = value;
        }
    }

    private bool _isDisposed;
    private readonly IClipboard _clipboard;
    private IInputRoot? _inputRoot;
    private WindowTransparencyLevel _transparencyLevel;
    
    /// <summary>
    /// Gets the input root.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public IInputRoot InputRoot => _inputRoot ?? throw new InvalidOperationException("InputRoot not set");


    /// <summary>
    ///  Creates a new instance of <see cref="MintyCoreTopLevelImpl"/>.
    /// </summary>
    /// <param name="platformGraphics"></param>
    /// <param name="clipboard"></param>
    /// <param name="compositor"></param>
    public MintyCoreTopLevelImpl(VkPlatformGraphics platformGraphics, IClipboard clipboard, Compositor compositor)
    {
        _platformGraphics = platformGraphics;
        Compositor = compositor;
        _clipboard = clipboard;
    }


    /// <inheritdoc />
    public Size ClientSize { get; private set; }

    /// <inheritdoc />
    public Size? FrameSize => null;

    /// <inheritdoc />
    public double RenderScaling { get; private set; } = 1.0;

    /// <inheritdoc />
    public IEnumerable<object> Surfaces => GetOrCreateSurfaces();

    /// <inheritdoc />
    public Action<RawInputEventArgs>? Input { get; set; }

    /// <inheritdoc />
    public Action<Rect>? Paint { get; set; }

    /// <inheritdoc />
    public Action<Size, WindowResizeReason>? Resized { get; set; }

    /// <inheritdoc />
    public Action<double>? ScalingChanged { get; set; }

    /// <inheritdoc />
    public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

    /// <inheritdoc />
    public Compositor Compositor { get; }

    /// <inheritdoc />
    public Action? Closed { get; set; }

    /// <inheritdoc />
    public Action? LostFocus { get; set; }

    private bool _redraw = true;

    /// <summary>
    /// Safely invoke the <see cref="Input"/> event.
    /// </summary>
    public void InvokeInput(RawInputEventArgs e) =>
        Dispatcher.UIThread.Invoke(() => Input?.Invoke(e), DispatcherPriority.Input);

    /// <summary>
    ///  Safely invoke the <see cref="Paint"/> event.
    /// </summary>
    public void InvokePaint(Rect rect) =>
        Dispatcher.UIThread.Invoke(() => Paint?.Invoke(rect), DispatcherPriority.Render);

    /// <summary>
    ///  Safely invoke the <see cref="Resized"/> event.
    /// </summary>
    public void InvokeResized(Size size, WindowResizeReason reason) =>
        Dispatcher.UIThread.Invoke(() => Resized?.Invoke(size, reason), DispatcherPriority.Render);

    /// <summary>
    ///  Safely invoke the <see cref="ScalingChanged"/> event.
    /// </summary>
    public void InvokeScalingChanged(double scaling) =>
        Dispatcher.UIThread.Invoke(() => ScalingChanged?.Invoke(scaling), DispatcherPriority.Render);

    /// <summary>
    ///  Safely invoke the <see cref="TransparencyLevelChanged"/> event.
    /// </summary>
    public void InvokeTransparencyLevelChanged(WindowTransparencyLevel transparencyLevel) =>
        Dispatcher.UIThread.Invoke(() => TransparencyLevelChanged?.Invoke(transparencyLevel),
            DispatcherPriority.Render);

    /// <summary>
    ///  Safely invoke the <see cref="Closed"/> event.
    /// </summary>
    public void InvokeClosed() => Dispatcher.UIThread.Invoke(() => Closed?.Invoke());
    
    /// <summary>
    ///  Safely invoke the <see cref="LostFocus"/> event.
    /// </summary>
    public void InvokeLostFocus() => Dispatcher.UIThread.Invoke(() => LostFocus?.Invoke());

    /// <inheritdoc />
    public WindowTransparencyLevel TransparencyLevel
    {
        get => _transparencyLevel;
        private set
        {
            if (_transparencyLevel.Equals(value))
                return;

            _transparencyLevel = value;
            InvokeTransparencyLevelChanged(value);
        }
    }

    /// <inheritdoc />
    public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; }

    /// <inheritdoc />
    public object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(IClipboard))
            return _clipboard;

        Log.Information("Feature {FeatureType} not supported", featureType);

        return null;
    }

    private VkSkiaSurface CreateSurface()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(MintyCoreTopLevelImpl));

        return _platformGraphics.GetSharedContext().CreateSurface(_renderSize, RenderScaling);
    }

    public Texture GetTexture() => GetOrCreateSurface().Texture;

    private VkSkiaSurface GetOrCreateSurface() => Surface ??= CreateSurface();

    private IEnumerable<object> GetOrCreateSurfaces() => [GetOrCreateSurface()];

    public void SetRenderScaling(double renderScaling) => SetRenderSize(_renderSize, renderScaling);

    public void SetRenderSize(PixelSize size) => SetRenderSize(size, RenderScaling);

    public void SetRenderSize(PixelSize renderSize, double renderScaling)
    {
        var scalingChanged = Math.Abs(RenderScaling - renderScaling) >= double.Epsilon;
        if (_renderSize == renderSize && !scalingChanged)
            return;

        _redraw = true;

        var oldClientSize = ClientSize;
        var unclampedClientSize = renderSize.ToSize(renderScaling);

        ClientSize = new Size(Math.Max(unclampedClientSize.Width, 0.0), Math.Max(unclampedClientSize.Height, 0.0));
        RenderScaling = renderScaling;

        //call this on the UI thread, to prevent errors when disposing the surface
        Dispatcher.UIThread.Invoke(() => ApplySetRenderSize(renderSize, scalingChanged, oldClientSize));
    }

    private void ApplySetRenderSize(PixelSize renderSize, bool scalingChanged, Size oldClientSize)
    {
        if (_renderSize != renderSize)
        {
            _renderSize = renderSize;

            if (Surface is not null)
            {
                Surface.Dispose();
                Surface = null;
            }

            if (_isDisposed)
                return;

            Surface = CreateSurface();
        }

        if (scalingChanged)
        {
            if (Surface != null)
                Surface.RenderScaling = RenderScaling;
            InvokeScalingChanged(RenderScaling);
        }

        if (oldClientSize != ClientSize)
            InvokeResized(ClientSize, scalingChanged ? WindowResizeReason.DpiChange : WindowResizeReason.Unspecified);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        Surface?.Dispose();
    }

    /// <inheritdoc />
    public void SetInputRoot(IInputRoot inputRoot) => _inputRoot = inputRoot;

    /// <inheritdoc />
    public Point PointToClient(PixelPoint point) => point.ToPoint(RenderScaling);

    /// <inheritdoc />
    public PixelPoint PointToScreen(Point point) => PixelPoint.FromPoint(point, RenderScaling);

    /// <inheritdoc />
    public void SetCursor(ICursorImpl? cursor)
    {
        //TODO implement along with cursor support
    }

    /// <inheritdoc />
    public IPopupImpl? CreatePopup() => null;

    /// <inheritdoc />
    public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
    {
        foreach (var transparencyLevel in transparencyLevels)
        {
            if (transparencyLevel == WindowTransparencyLevel.Transparent ||
                transparencyLevel == WindowTransparencyLevel.None)
            {
                TransparencyLevel = transparencyLevel;
                return;
            }
        }
    }

    /// <inheritdoc />
    public void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
    {
    }

    public void OnDraw()
    {
        if (!_redraw) return;
        _redraw = false;

        var rect = new Rect(0, 0, ClientSize.Width, ClientSize.Height);
        InvokePaint(rect);
    }
}