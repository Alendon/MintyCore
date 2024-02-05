using MintyCore.Graphics;
using MintyCore.Registries;
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
                ColorAttachments = new[]
                {
                    new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                },
                PipelineBindPoint = PipelineBindPoint.Graphics,
            }
        },
        new SubpassDependency[]
        {
            new SubpassDependency
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

    [RegisterDescriptorSet("buffer_bind")]
    public static DescriptorSetInfo BufferBind => new()
    {
        Bindings =
        [
            new DescriptorSetLayoutBinding()
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.StorageBuffer,
                StageFlags = ShaderStageFlags.VertexBit
            }
        ],
        DescriptorSetsPerPool = 100
    };
}