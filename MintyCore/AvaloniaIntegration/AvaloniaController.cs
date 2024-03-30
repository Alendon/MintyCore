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
using Silk.NET.Vulkan;

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
        
        Dispatcher.UIThread.MainLoop(_uiThreadCts!.Token);
        
        // Cleanup. This is called after the cancellation token is cancelled
        //Must happen on the UI thread
        _topLevel?.Impl.Dispose();
        uiPlatform.Dispose();
        _topLevel?.Dispose();
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
            var textureDescription = TextureDescription.Texture2D(uiTexture.Width, uiTexture.Height, 1, 1, Format.R8G8B8A8Unorm,
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