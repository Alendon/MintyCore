using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

namespace MintyCore.UI;

[RegisterRenderModule("ui")]
public class UiRenderModule(IVulkanEngine vulkanEngine, IPipelineManager pipelineManager)
    : RenderModule
{
    private Func<UiIntermediateData?> _dataAccessor = null!;
    private Vk Vk => vulkanEngine.Vk;


    public override void Setup()
    {
        ModuleDataAccessor.SetColorAttachment(RenderDataIDs.UiOutput, this);
        _dataAccessor = ModuleDataAccessor.UseIntermediateData<UiIntermediateData>(IntermediateRenderDataIDs.Ui, this);
    }

    [RegisterRenderTexture("ui_output")]
    public static RenderTextureDescription UiTexture => new(new Swapchain(), Format.R8G8B8A8Unorm);

    public override void Render(ManagedCommandBuffer commandBuffer)
    {
        var data = _dataAccessor();

        if (data?.InputData is null) return;

        var pipeline = pipelineManager.GetPipeline(PipelineIDs.UiPipeline);
        var pipelineLayout = pipelineManager.GetPipelineLayout(PipelineIDs.UiPipeline);

        var renderData = data.InputData;

        Span<DescriptorSet> descriptorBind = stackalloc DescriptorSet[]
        {
            default,
            data.TransformDescriptorSet
        };

        var cb = commandBuffer.InternalCommandBuffer;

        foreach (var batch in renderData)
        {
            descriptorBind[0] = batch.Texture.SampledImageDescriptorSet;
            var scissor = batch.Scissor.ToRect2D();
            var viewport = new Viewport(0, 0, vulkanEngine.SwapchainExtent.Width, vulkanEngine.SwapchainExtent.Height,
                0, 1);

            for (var index = 0; index < batch.Data.Length; index++)
            {
                Vk.CmdBindPipeline(cb, PipelineBindPoint.Graphics, pipeline);
                Vk.CmdBindDescriptorSets(cb, PipelineBindPoint.Graphics, pipelineLayout, 0, descriptorBind, default);

                Vk.CmdSetScissor(cb, 0, 1, scissor);
                Vk.CmdSetViewport(cb, 0, 1, viewport);

                Vk.CmdPushConstants(cb, pipelineLayout, ShaderStageFlags.VertexBit, 0,
                    MemoryMarshal.AsBytes(batch.Data.Slice(index, 1)));

                Vk.CmdDraw(cb, 6, 1, 0, 0);
            }
        }
    }

    public override Identification Identification => RenderModuleIDs.Ui;

    public override void Dispose()
    {
    }
}