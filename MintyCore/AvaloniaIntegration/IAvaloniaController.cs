using Avalonia;
using MintyCore.Graphics.VulkanObjects;

namespace MintyCore.AvaloniaIntegration;

public interface IAvaloniaController
{
    void SetupAndRun();
    void Stop();
    
    void Draw(Rect rect);
    Texture GetTexture();
    MintyCoreTopLevel TopLevel { get; }
}