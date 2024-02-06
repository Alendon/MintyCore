using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using PipelineIDs = TestMod.Identifications.PipelineIDs;
using RenderModuleIDs = TestMod.Identifications.RenderModuleIDs;

namespace TestMod.Render;

[RegisterRenderModule("fill_ui")]
public class FillUiRenderModule(IVulkanEngine vulkanEngine, IPipelineManager pipelineManager) : RenderModule
{
    private Func<DescriptorSet> _uiOutput = null!;

    private Vk Vk => vulkanEngine.Vk;

    public override IEnumerable<Identification> ExecuteAfter =>
    [
        RenderModuleIDs.TriangleRender,
        MintyCore.Identifications.RenderModuleIDs.Ui
    ];

    public override void Setup()
    {
        ModuleDataAccessor.SetColorAttachment(new Swapchain(), this);
        _uiOutput = ModuleDataAccessor.UseSampledTexture(RenderDataIDs.UiOutput, this);
    }

    public override unsafe void Render(ManagedCommandBuffer commandBuffer)
    {
        var uiOutput = _uiOutput();
        if (uiOutput.Handle == default) return;

        var swapchainExtent = vulkanEngine.SwapchainExtent;
        var viewport = new Viewport()
        {
            Height = swapchainExtent.Height,
            Width = swapchainExtent.Width,
            MaxDepth = 1
        };
        var scissor = new Rect2D(default, swapchainExtent);

        var cb = commandBuffer.InternalCommandBuffer;

        Vk.CmdSetViewport(cb, 0, 1, viewport);
        Vk.CmdSetScissor(cb, 0, 1, scissor);

        Vk.CmdBindDescriptorSets(cb, PipelineBindPoint.Graphics, pipelineManager.GetPipelineLayout(PipelineIDs.FillUi),
            0, 1, uiOutput, 0, null);
        
        var pipeline = pipelineManager.GetPipeline(PipelineIDs.FillUi);
        Vk.CmdBindPipeline(cb, PipelineBindPoint.Graphics, pipeline);
        Vk.CmdDraw(cb, 6, 1, 0, 0);
    }

    public override Identification Identification => RenderModuleIDs.FillUi;

    public override void Dispose()
    {
    }
}