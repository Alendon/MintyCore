using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;
using Silk.NET.Vulkan;
using static MintyCore.Render.VulkanEngine;

namespace MintyCore.Render;

public readonly struct Material
{
    public readonly Identification MaterialId;

    public readonly Pipeline Pipeline;
    public readonly PipelineLayout PipelineLayout;

    public readonly UnmanagedArray<(DescriptorSet, uint)> DescriptorSets;

    public Material(Identification materialId, Pipeline pipeline, PipelineLayout pipelineLayout,
        UnmanagedArray<(DescriptorSet, uint)> descriptorSets)
    {
        MaterialId = materialId;
        Pipeline = pipeline;
        PipelineLayout = pipelineLayout;
        DescriptorSets = descriptorSets;
    }

    public void Bind(CommandBuffer buffer)
    {
        VulkanEngine.Vk.CmdBindPipeline(buffer, PipelineBindPoint.Graphics, Pipeline);
        foreach (var (descriptorSet, bindingPoint) in DescriptorSets)
            VulkanEngine.Vk.CmdBindDescriptorSets(buffer, PipelineBindPoint.Graphics, PipelineLayout, bindingPoint,
                1u,
                in descriptorSet, 0, 0);
        var viewport = new Viewport
        {
            Width = SwapchainExtent.Width,
            Height = SwapchainExtent.Height,
            MaxDepth = 1f
        };
        var scissor = new Rect2D
        {
            Extent = SwapchainExtent,
            Offset = new Offset2D(0, 0)
        };

        VulkanEngine.Vk.CmdSetViewport(buffer, 0, 1, viewport);
        VulkanEngine.Vk.CmdSetScissor(buffer, 0, 1, scissor);
    }
}