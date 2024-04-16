using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Registries;
using MintyCore.Utils;
using Serilog;
using Silk.NET.Vulkan;
using TestMod.Identifications;

namespace TestMod.Render;

[RegisterRenderModule("triangle_render")]
public class TriangleRender : RenderModule
{
    public required IVulkanEngine VulkanEngine { private get; [UsedImplicitly] set; }
    public required IPipelineManager PipelineManager { private get; [UsedImplicitly] set; }

    Func<TriangleMeshData?>? _triangleMeshData;

    public override IEnumerable<Identification> ExecuteBefore { get; } = [MintyCore.Identifications.RenderModuleIDs.AvaloniaUi];

    public override void Setup()
    {
        _triangleMeshData =
            ModuleDataAccessor.UseIntermediateData<TriangleMeshData>(IntermediateRenderDataIDs.TriangleMeshData, this);
        
        ModuleDataAccessor.SetColorAttachment(new Swapchain(), this);
    }

    public override void Render(ManagedCommandBuffer commandBuffer)
    {
        if (_triangleMeshData is null)
        {
            Log.Error("TriangleMeshData is null");
            return;
        }

        var triangleMeshData = _triangleMeshData();
        if(triangleMeshData is null)
        {
            return;
        }
        
        if (triangleMeshData.TriangleCount == 0)
        {
            return;
        }

        var swapchainExtent = VulkanEngine.SwapchainExtent;
        var viewport = new Viewport
        {
            Height = swapchainExtent.Height,
            Width = swapchainExtent.Width,
            MaxDepth = 1
        };

        var scissor = new Rect2D
        {
            Extent = swapchainExtent,
            Offset = new Offset2D(0, 0)
        };

        var internalBuffer = commandBuffer.InternalCommandBuffer;
        var pipeline = PipelineManager.GetPipeline(PipelineIDs.Triangle);
        var pipelineLayout = PipelineManager.GetPipelineLayout(PipelineIDs.Triangle);
        var descriptorSet = triangleMeshData.BufferDescriptor;
        
        VulkanEngine.Vk.CmdBindPipeline(internalBuffer, PipelineBindPoint.Graphics, pipeline);

        VulkanEngine.Vk.CmdSetViewport(internalBuffer, 0, 1, viewport);
        VulkanEngine.Vk.CmdSetScissor(internalBuffer, 0, 1, scissor);

        VulkanEngine.Vk.CmdBindDescriptorSets(internalBuffer, PipelineBindPoint.Graphics, pipelineLayout, 0, 1,
            descriptorSet, 0, 0);

        VulkanEngine.Vk.CmdDraw(internalBuffer, 3, (uint)triangleMeshData.TriangleCount, 0, 0);
    }

    public override Identification Identification => RenderModuleIDs.TriangleRender;

    public override void Dispose()
    {
    }
}