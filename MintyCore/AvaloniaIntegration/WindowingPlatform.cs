using Avalonia.Platform;

namespace MintyCore.AvaloniaIntegration;

//TODO Future: Do we need to properly implement this?

internal class WindowingPlatform : IWindowingPlatform
{
    public IWindowImpl CreateWindow()
    {
        throw new System.NotImplementedException("Sub windows aren't implemented yet");
    }

    public IWindowImpl CreateEmbeddableWindow()
    {
        throw new System.NotImplementedException("Sub windows aren't implemented yet");
    }

    public ITrayIconImpl? CreateTrayIcon()
    {
        return null;
    }
}