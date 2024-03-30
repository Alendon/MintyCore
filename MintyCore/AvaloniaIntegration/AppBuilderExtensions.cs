using System.IO;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Platform;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Modding;

namespace MintyCore.AvaloniaIntegration;

public static class AppBuilderExtensions
{
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
    
    public static AppBuilder LogToSerilog(this AppBuilder builder)
    {
        Logger.Sink = new SerilogSink();
        return builder;
    }


    public static AppBuilder UseMintyCoreIdePreview(this AppBuilder builder)
        => builder
            .UseStandardRuntimePlatformSubsystem()
            .UseSkia()
            .LogToTrace(LogEventLevel.Debug)
            .AfterSetup(_ =>
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                File.WriteAllText(@"D:\currentDirectory.txt", currentDirectory);
            });
}