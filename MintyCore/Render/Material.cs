using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;
using Silk.NET.Vulkan;
using static MintyCore.Render.VulkanEngine;

namespace MintyCore.Render;

/// <summary>
/// Material struct used for rendering
/// </summary>
public readonly struct Material
{
    /// <summary>
    /// The <see cref="Identification"/> of this Material. May be default if not a registered material
    /// </summary>
    public readonly Identification MaterialId;

    /// <summary>
    /// The <see cref="Silk.NET.Vulkan.Pipeline"/> of this material
    /// </summary>
    public readonly Pipeline Pipeline;
    /// <summary>
    /// The <see cref="Silk.NET.Vulkan.PipelineLayout"/> of this material
    /// </summary>
    public readonly PipelineLayout PipelineLayout;

    /// <summary>
    /// The descriptor sets used in this material
    /// </summary>
    public readonly UnmanagedArray<(DescriptorSet, uint)> DescriptorSets;

    /// <summary>
    /// Material constructor
    /// </summary>
    /// <param name="materialId">Material Identification. May be default</param>
    /// <param name="pipeline">Pipeline to use in the material</param>
    /// <param name="pipelineLayout">Pipeline layout to use in the material. Must match the pipeline</param>
    /// <param name="descriptorSets">DescriptorSets used in the material. May be default</param>
    public Material(Identification materialId, Pipeline pipeline, PipelineLayout pipelineLayout,
        UnmanagedArray<(DescriptorSet, uint)> descriptorSets)
    {
        MaterialId = materialId;
        Pipeline = pipeline;
        PipelineLayout = pipelineLayout;
        DescriptorSets = descriptorSets;
    }

    /// <summary>
    /// Bind the material to the command buffer
    /// </summary>
    /// <param name="buffer"></param>
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