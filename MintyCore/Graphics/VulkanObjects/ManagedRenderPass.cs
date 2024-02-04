using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.VulkanObjects;

public class ManagedRenderPass : VulkanObject
{
    public RenderPass InternalRenderPass { get; }
    public uint SubpassCount { get; }

    public ManagedRenderPass(IVulkanEngine vulkanEngine) : base(vulkanEngine)
    {
    }

    public ManagedRenderPass(IVulkanEngine vulkanEngine, IAllocationHandler? allocationHandler) : base(vulkanEngine, allocationHandler)
    {
    }
}