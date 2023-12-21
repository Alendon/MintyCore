using MintyCore.Registries;
using MintyCore.Render;
using Silk.NET.Vulkan;

namespace TestMod.Render;

public static class RenderObjects
{
    [RegisterRenderPass("main")]
    public static RenderPassInfo Main(IVulkanEngine vulkanEngine) => new(
        new AttachmentDescription[]
        {
            new()
            {
                Format = vulkanEngine.SwapchainImageFormat,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                Samples = SampleCountFlags.Count1Bit
            }
        },
        new SubpassDescriptionInfo[]
        {
            new()
            {
                ColorAttachments = new []
                {
                    new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                },
                PipelineBindPoint = PipelineBindPoint.Graphics,
            }
        },
        new SubpassDependency[]
        {
            new()
            {
               SrcSubpass = Vk.SubpassExternal,
               DstSubpass = 0,
               SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
               DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
               SrcAccessMask = AccessFlags.NoneKhr,
               DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.ColorAttachmentReadBit
            }
        },
        RenderPassCreateFlags.None
    );
}