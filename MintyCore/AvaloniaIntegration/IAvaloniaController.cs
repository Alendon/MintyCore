using MintyCore.Graphics.VulkanObjects;

namespace MintyCore.AvaloniaIntegration;

public interface IAvaloniaController
{
    void SetupAndRun();
    void Stop();
    
    Texture Draw(Texture? texture);
    MintyCoreTopLevel TopLevel { get; }
}