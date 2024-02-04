using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.VulkanObjects;

public class ManagedPipeline : VulkanObject
{
    public Pipeline InternalPipeline { get; }
    public PipelineLayout InternalPipelineLayout { get; }

    public ManagedPipeline(IVulkanEngine vulkanEngine) : base(vulkanEngine)
    {
        
    }

    public ManagedPipeline(IVulkanEngine vulkanEngine, IAllocationHandler? allocationHandler) : base(vulkanEngine, allocationHandler)
    {
    }
}