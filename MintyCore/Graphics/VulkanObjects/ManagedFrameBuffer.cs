using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.VulkanObjects;

public class ManagedFrameBuffer : VulkanObject
{
    public Framebuffer InternalFrameBuffer { get; }

    public ManagedFrameBuffer(IVulkanEngine vulkanEngine, Framebuffer internalFrameBuffer) : base(vulkanEngine)
    {
        InternalFrameBuffer = internalFrameBuffer;
    }

    public ManagedFrameBuffer(IVulkanEngine vulkanEngine, IAllocationHandler allocationHandler,
        Framebuffer internalFrameBuffer) : base(vulkanEngine, allocationHandler)
    {
        InternalFrameBuffer = internalFrameBuffer;
    }
}