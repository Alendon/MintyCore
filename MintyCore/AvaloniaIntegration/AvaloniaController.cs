using System;
using System.Threading;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Threading;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Modding;
using MintyCore.Utils;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace MintyCore.AvaloniaIntegration;

[Singleton<IAvaloniaController>(SingletonContextFlags.NoHeadless)]
internal class AvaloniaController(
    IUiPlatform uiPlatform,
    IModManager modManager,
    IVulkanEngine vulkanEngine,
    ITextureManager textureManager
) : IAvaloniaController
{
    private MintyCoreTopLevel? _topLevel;
    private Thread? _uiThread;
    private CancellationTokenSource? _uiThreadCts;

    public MintyCoreTopLevel TopLevel => _topLevel ?? throw new InvalidOperationException("Not initialized");

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
    }

    public void Stop()
    {
        _uiThreadCts?.Cancel();
        _uiThread?.Join();
        
        _uiThreadCts?.Dispose();
        _uiThreadCts = null;
        
        _uiThread = null;
        
        _topLevel?.Impl.Dispose();
        uiPlatform.Dispose();
        _topLevel?.Dispose();
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
        
        Dispatcher.UIThread.MainLoop(_uiThreadCts!.Token);
    }

    public void Draw(Rect rect)
    {
        var task = TopLevel.Impl.OnDraw(rect);
        uiPlatform.TriggerRender();
        task.Wait();
    }

    public Texture GetTexture()
    {
        return TopLevel.Impl.GetTexture();
    }


    private void OnWindowResized(Vector2D<int> vector2D)
    {
        TopLevel.Impl.SetRenderSize(new PixelSize(vector2D.X, vector2D.Y));
    }
}