using Silk.NET.Vulkan;

namespace MintyCore.Render.VulkanObjects;

public class ManagedPipeline //: VulkanObject
{
    public Pipeline InternalPipeline { get; }
    public PipelineLayout InternalPipelineLayout { get; }
}