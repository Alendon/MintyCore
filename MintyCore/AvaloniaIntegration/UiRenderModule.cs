using System;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Utils;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.AvaloniaIntegration;

[RegisterRenderModule("avalonia_ui")]
public class UiRenderModule(IPipelineManager pipelineManager, IVulkanEngine vulkanEngine) : RenderModule
{
    private Func<UiIntermediateData?>? _intermediateGetter;

    public override void Setup()
    {
        _intermediateGetter =
            ModuleDataAccessor.UseIntermediateData<UiIntermediateData>(IntermediateRenderDataIDs.AvaloniaUi, this);
        ModuleDataAccessor.SetColorAttachment(new Swapchain(), this);
    }

    public override unsafe void Render(ManagedCommandBuffer commandBuffer)
    {
        if (_intermediateGetter?.Invoke() is not { } intermediateData)
        {
            return;
        }

        var cb = commandBuffer.InternalCommandBuffer;
        var vk = vulkanEngine.Vk;

        var pipeline = pipelineManager.GetPipeline(PipelineIDs.UiPipeline);
        var pipelineLayout = pipelineManager.GetPipelineLayout(PipelineIDs.UiPipeline);

        vk.CmdBindPipeline(cb, PipelineBindPoint.Graphics, pipeline);
        vk.CmdBindDescriptorSets(cb, PipelineBindPoint.Graphics, pipelineLayout, 0, 1, intermediateData.DescriptorSet,
            0, null);

        vk.CmdSetScissor(cb, 0, 1, new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = vulkanEngine.SwapchainExtent
        });

        vk.CmdSetViewport(cb, 0, 1, new Viewport
        {
            X = 0,
            Y = 0,
            Width = vulkanEngine.SwapchainExtent.Width,
            Height = vulkanEngine.SwapchainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1
        });

        vk.CmdDraw(cb, 6, 2, 0, 0);
    }

    public override Identification Identification => RenderModuleIDs.AvaloniaUi;

    public override void Dispose()
    {
    }
}