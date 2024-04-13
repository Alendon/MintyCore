using Avalonia;
using Avalonia.Logging;
using Avalonia.Platform;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Modding;

namespace MintyCore.AvaloniaIntegration;

/// <summary>
///  Extensions for <see cref="AppBuilder"/>.
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    ///   Use MintyCore with the provided platform, mod manager, vulkan engine, and texture manager.
    /// </summary>
    public static AppBuilder UseMintyCore(this AppBuilder builder, IUiPlatform platform, IModManager modManager,
        IVulkanEngine vulkanEngine, ITextureManager textureManager)
        => builder
            .UseStandardRuntimePlatformSubsystem()
            .UseSkia()
            .UseWindowingSubsystem(() => platform.Initialize(vulkanEngine, textureManager))
            .LogToSerilog()
            .AfterSetup(_ =>
            {
                var originalLoader = AvaloniaLocator.CurrentMutable.GetService<IAssetLoader>();
                AvaloniaLocator.CurrentMutable.Bind<IAssetLoader>()
                    .ToConstant(new ModAssetLoader(originalLoader, modManager));
            });
    
    /// <summary>
    ///  Redirect Avalonia logging to Serilog.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static AppBuilder LogToSerilog(this AppBuilder builder)
    {
        Logger.Sink = new SerilogSink();
        return builder;
    }


    /// <summary>
    ///  Use MintyCore in the IDE preview configuration.
    /// </summary>
    public static AppBuilder UseMintyCoreIdePreview(this AppBuilder builder, string modProjectPath)
        => builder
            .UseStandardRuntimePlatformSubsystem()
            .UseSkia()
            .AfterSetup(_ =>
            {
                var originalLoader = AvaloniaLocator.CurrentMutable.GetService<IAssetLoader>();
                AvaloniaLocator.CurrentMutable.Bind<IAssetLoader>()
                    .ToConstant(new PreviewAssetLoader(originalLoader, modProjectPath));
            });
}