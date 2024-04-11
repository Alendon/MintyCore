using System;
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

[Singleton<IUiPlatform>(SingletonContextFlags.NoHeadless)]
public class MintyCorePlatform : IUiPlatform
{
    private IKeyboardDevice _keyboardDevice = new KeyboardDevice();
    private ManualRenderTimer _renderTimer = new();

    public void Initialize(IVulkanEngine vulkanEngine, ITextureManager textureManager)
    {
        _gpu = new VkSkiaGpu(vulkanEngine, textureManager);
        var graphics = new VkPlatformGraphics(_gpu);

        AvaloniaLocator.CurrentMutable
            .Bind<IClipboard>().ToConstant(new Clipboard())
            .Bind<ICursorFactory>().ToConstant(new CursorFactory())
            .Bind<IDispatcherImpl>().ToConstant(new ManagedDispatcherImpl(null))
            .Bind<IKeyboardDevice>().ToConstant(_keyboardDevice)
            .Bind<IPlatformGraphics>().ToConstant(graphics)
            .Bind<IPlatformIconLoader>().ToConstant(new PlatformIconLoader())
            .Bind<IPlatformSettings>().ToConstant(new PlatformSettings())
            .Bind<IRenderTimer>().ToConstant(_renderTimer)
            .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatform())
            .Bind<PlatformHotkeyConfiguration>().ToConstant(CreatePlatformHotKeyConfiguration());

        _compositor = new Compositor(graphics);
    }

    public void TriggerRender()
    {
        _renderTimer.TriggerTick();
    }

    private Compositor? _compositor;
    private VkSkiaGpu? _gpu;

    public Compositor Compositor => _compositor ??
                                    throw new InvalidOperationException(
                                        $"{nameof(MintyCorePlatform)} hasn't been initialized");

    private static PlatformHotkeyConfiguration CreatePlatformHotKeyConfiguration()
        => OperatingSystem.IsMacOS()
            ? new PlatformHotkeyConfiguration(commandModifiers: KeyModifiers.Meta,
                wholeWordTextActionModifiers: KeyModifiers.Alt)
            : new PlatformHotkeyConfiguration(commandModifiers: KeyModifiers.Control);

    public void Dispose()
    {
        _gpu?.Dispose();;
        _gpu = null;
    }
}