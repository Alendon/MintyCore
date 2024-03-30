using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

public class MintyCoreTopLevelImpl : ITopLevelImpl
{
    private readonly VkPlatformGraphics _platformGraphics;

    private PixelSize _renderSize;
    private VkSkiaSurface? _surface;
    private bool _isDisposed;
    private readonly IClipboard _clipboard;
    private IInputRoot? _inputRoot;
    private WindowTransparencyLevel _transparencyLevel;


    public MintyCoreTopLevelImpl(VkPlatformGraphics platformGraphics, IClipboard clipboard, Compositor compositor)
    {
        _platformGraphics = platformGraphics;
        Compositor = compositor;
        _clipboard = clipboard;
    }


    public Size ClientSize { get; private set; }
    public Size? FrameSize => null;
    public double RenderScaling { get; private set; } = 1.0;
    public IEnumerable<object> Surfaces => GetOrCreateSurfaces();

    public Action<RawInputEventArgs>? Input { get; set; }
    public Action<Rect>? Paint { get; set; }
    public Action<Size, WindowResizeReason>? Resized { get; set; }
    public Action<double>? ScalingChanged { get; set; }
    public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
    public Compositor Compositor { get; }
    public Action? Closed { get; set; }
    public Action? LostFocus { get; set; }

    public WindowTransparencyLevel TransparencyLevel
    {
        get => _transparencyLevel;
        private set {
            if (_transparencyLevel.Equals(value))
                return;

            _transparencyLevel = value;
            TransparencyLevelChanged?.Invoke(value);
        }
    }

    public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; }

    public object? TryGetFeature(Type featureType)
    {
        if(featureType == typeof(IClipboard))
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

    private VkSkiaSurface GetOrCreateSurface() => _surface ??= CreateSurface();

    private IEnumerable<object> GetOrCreateSurfaces() => [GetOrCreateSurface()];

    public void SetRenderScaling(double renderScaling) => SetRenderSize(_renderSize, renderScaling);

    public void SetRenderSize(PixelSize size) => SetRenderSize(size, RenderScaling);

    public void SetRenderSize(PixelSize renderSize, double renderScaling)
    {
        var scalingChanged = Math.Abs(RenderScaling - renderScaling) >= double.Epsilon;
        if (_renderSize == renderSize && !scalingChanged)
            return;

        var oldClientSize = ClientSize;
        var unclampedClientSize = renderSize.ToSize(renderScaling);

        ClientSize = new Size(Math.Max(unclampedClientSize.Width, 0.0), Math.Max(unclampedClientSize.Height, 0.0));
        RenderScaling = renderScaling;

        if (_renderSize != renderSize)
        {
            _renderSize = renderSize;

            if (_surface is not null)
            {
                _surface.Dispose();
                _surface = null;
            }

            if (_isDisposed)
                return;

            _surface = CreateSurface();
        }

        if (scalingChanged)
        {
            if (_surface != null)
                _surface.RenderScaling = RenderScaling;
            ScalingChanged?.Invoke(RenderScaling);
        }

        if (oldClientSize != ClientSize)
            Resized?.Invoke(ClientSize, scalingChanged ? WindowResizeReason.DpiChange : WindowResizeReason.Unspecified);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _surface?.Dispose();
    }

    public void SetInputRoot(IInputRoot inputRoot) => _inputRoot = inputRoot;

    public Point PointToClient(PixelPoint point) => point.ToPoint(RenderScaling);

    public PixelPoint PointToScreen(Point point) => PixelPoint.FromPoint(point, RenderScaling);

    public void SetCursor(ICursorImpl? cursor)
    {
        //TODO implement along with cursor support
    }

    public IPopupImpl? CreatePopup() => null;

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

    public void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
    {
    }

    public Task OnDraw(Rect rect)
    {
        var res = Dispatcher.UIThread.InvokeAsync(
            () => Paint?.Invoke(rect), DispatcherPriority.Render);

        return res.GetTask();
    }
}