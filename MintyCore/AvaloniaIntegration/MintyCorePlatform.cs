﻿using System;
using Avalonia;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Utils;

namespace MintyCore.AvaloniaIntegration;

/// <inheritdoc />
[Singleton<IUiPlatform>(SingletonContextFlags.NoHeadless)]
public class MintyCorePlatform : IUiPlatform
{
    private readonly IKeyboardDevice _keyboardDevice = new KeyboardDevice();
    private readonly ManualRenderTimer _renderTimer = new();

    /// <inheritdoc />
    public void Initialize(IVulkanEngine vulkanEngine, ITextureManager textureManager)
    {
        _gpu = new VkSkiaGpu(vulkanEngine, textureManager);
        var graphics = new VkPlatformGraphics(_gpu);

        AvaloniaLocator.CurrentMutable
            .Bind<IClipboard>().ToConstant(new Clipboard())
            .Bind<ICursorFactory>().ToConstant(new CursorFactory())
            .Bind<IDispatcherImpl>().ToConstant(new ManagedDispatcherImpl(null))
            .Bind<IKeyboardDevice>().ToConstant(_keyboardDevice)
            .Bind<IMouseDevice>().ToConstant(new MouseDevice(new Pointer(0, PointerType.Mouse, true)))
            .Bind<IPlatformGraphics>().ToConstant(graphics)
            .Bind<IPlatformIconLoader>().ToConstant(new PlatformIconLoader())
            .Bind<IPlatformSettings>().ToConstant(new PlatformSettings())
            .Bind<IRenderTimer>().ToConstant(_renderTimer)
            .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatform())
            .Bind<PlatformHotkeyConfiguration>().ToConstant(CreatePlatformHotKeyConfiguration());

        _compositor = new Compositor(graphics);
    }

    /// <inheritdoc />
    public void TriggerRender()
    {
        _renderTimer.TriggerTick();
    }

    private Compositor? _compositor;
    private VkSkiaGpu? _gpu;

    /// <inheritdoc />
    public Compositor Compositor => _compositor ??
                                    throw new InvalidOperationException(
                                        $"{nameof(MintyCorePlatform)} hasn't been initialized");

    private static PlatformHotkeyConfiguration CreatePlatformHotKeyConfiguration()
        => OperatingSystem.IsMacOS()
            ? new PlatformHotkeyConfiguration(commandModifiers: KeyModifiers.Meta,
                wholeWordTextActionModifiers: KeyModifiers.Alt)
            : new PlatformHotkeyConfiguration(commandModifiers: KeyModifiers.Control);

    /// <inheritdoc />
    public void Dispose()
    {
        _gpu?.Dispose();;
        _gpu = null;
    }
}