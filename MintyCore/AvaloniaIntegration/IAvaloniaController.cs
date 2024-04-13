using MintyCore.Graphics.VulkanObjects;
using Silk.NET.GLFW;

namespace MintyCore.AvaloniaIntegration;

public interface IAvaloniaController
{
    void SetupAndRun();
    void Stop();
    
    Texture Draw(Texture? texture);
    MintyCoreTopLevel TopLevel { get; }
    
    void TriggerScroll(float deltaX, float deltaY);
    void TriggerCursorPos(float x, float y);
    void TriggerMouseButton(MouseButton button, InputAction action, KeyModifiers mods);
    void TriggerKey(Key physicalKey, InputAction action, KeyModifiers keyModifiers, string? localizedKeyRep);
    void TriggerChar(char character);
}