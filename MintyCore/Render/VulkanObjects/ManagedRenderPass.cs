using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.VulkanObjects;

public class ManagedRenderPass //: VulkanObject
{
    public RenderPass InternalRenderPass { get; }
    public uint SubpassCount { get; }
}