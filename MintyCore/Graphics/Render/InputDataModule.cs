using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render;

public abstract class InputDataModule
{
    public abstract void Setup();
    public abstract void Update(CommandBuffer commandBuffer);
}