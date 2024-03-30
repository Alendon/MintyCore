using System.IO;
using Avalonia.Platform;

namespace MintyCore.AvaloniaIntegration;

//TODO Future create a real implementation

internal class PlatformIconLoader : IPlatformIconLoader
{
    
    public IWindowIconImpl LoadIcon(string fileName)
    {
        return new StubWindowIconImpl();
    }

    public IWindowIconImpl LoadIcon(Stream stream)
    {
        return new StubWindowIconImpl();
    }

    public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
    {
        return new StubWindowIconImpl();
    }
    
    class StubWindowIconImpl : IWindowIconImpl
    {
        public void Save(Stream outputStream)
        {
        }
    }
}