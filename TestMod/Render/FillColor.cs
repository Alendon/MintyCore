using JetBrains.Annotations;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.VulkanObjects;
using Serilog;
using Silk.NET.Vulkan;
using TestMod.Identifications;

namespace TestMod.Render;

public sealed class FillColor : IRenderModule
{
    public required IRenderPassManager RenderPassManager { private get; [UsedImplicitly] init; }
    public required IPipelineManager PipelineManager { private get; [UsedImplicitly] init; }
    public required IVulkanEngine VulkanEngine { private get; [UsedImplicitly] init; }
    private Vk Vk => VulkanEngine.Vk;

    /// <inheritdoc />
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Process(ManagedCommandBuffer cb)
    {
        if (_framebufferBuilder is null)
        {
            Log.Error("Framebuffer input is null");
            return;
        }

        RenderPassBeginInfo beginInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            Framebuffer = _framebufferBuilder.GetConcreteResult(),
            RenderPass = RenderPassManager.GetRenderPass(RenderPassIDs.Main),
            RenderArea = new Rect2D(new Offset2D(0, 0), VulkanEngine.SwapchainExtent)
        };

        Vk.CmdBeginRenderPass(cb, beginInfo, SubpassContents.Inline);

        Render(cb);

        Vk.CmdEndRenderPass(cb);
    }

    private void Render(ManagedCommandBuffer cb)
    {
        var swapchainExtent = VulkanEngine.SwapchainExtent;
        var viewport = new Viewport()
        {
            Height = swapchainExtent.Height,
            Width = swapchainExtent.Width,
            MaxDepth = 1
        };
        var scissor = new Rect2D(default, swapchainExtent);

        VulkanEngine.Vk.CmdSetViewport(cb, 0, 1, viewport);
        VulkanEngine.Vk.CmdSetScissor(cb, 0, 1, scissor);

        var pipeline = PipelineManager.GetPipeline(PipelineIDs.Background);
        VulkanEngine.Vk.CmdBindPipeline(cb, PipelineBindPoint.Graphics, pipeline);
        VulkanEngine.Vk.CmdDraw(cb, 6, 1, 0, 0);
    }

    /// <inheritdoc />
    public void Initialize(IRenderWorker renderWorker)
    {
        /*renderWorker.SetInputDependencyNew<BuildFramebuffer>(RenderModuleIDs.FillColor, RenderInputIDs.BuildFramebuffer,
            SetFramebuffer);*/
    }

    private BuildFramebuffer? _framebufferBuilder;

    private void SetFramebuffer(BuildFramebuffer framebuffer)
    {
        _framebufferBuilder = framebuffer;
    }
}