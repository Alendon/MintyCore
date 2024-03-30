using System;
using Avalonia.Rendering.Composition;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;

namespace MintyCore.AvaloniaIntegration;

public interface IUiPlatform: IDisposable
{
    void Initialize(IVulkanEngine vulkanEngine, ITextureManager textureManager);
    void TriggerRender();
    
    Compositor Compositor { get; }
}