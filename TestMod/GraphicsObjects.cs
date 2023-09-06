using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using TestMod.Identifications;

namespace TestMod;

public static class GraphicsObjects
{
    [RegisterShader("triangle_vert", "triangle_vert.spv")]
    public static ShaderInfo TriangleVert => new(ShaderStageFlags.VertexBit);

    [RegisterShader("triangle_frag", "triangle_frag.spv")]
    public static ShaderInfo TriangleFrag => new(ShaderStageFlags.FragmentBit);

    [RegisterGraphicsPipeline("triangle")]
    public static GraphicsPipelineDescription TrianglePipeline
    {
        get
        {
            return new GraphicsPipelineDescription()
            {
                Flags = PipelineCreateFlags.None,
                RenderPass = RenderPassIDs.Main,
                Scissors = new Rect2D[]
                {
                    new()
                    {
                        Extent = VulkanEngine.SwapchainExtent,
                        Offset = new Offset2D(0, 0)
                    }
                },
                Viewports = new Viewport[]
                {
                    new()
                    {
                        Height = VulkanEngine.SwapchainExtent.Height,
                        Width = VulkanEngine.SwapchainExtent.Width,
                        MaxDepth = 1
                    }
                },
                Shaders = new[]
                {
                    ShaderIDs.TriangleVert,
                    ShaderIDs.TriangleFrag
                },
                Topology = PrimitiveTopology.TriangleList,
                SampleCount = SampleCountFlags.Count1Bit,
                DynamicStates = new[]
                {
                    DynamicState.Scissor,
                    DynamicState.Viewport
                },
                RasterizationInfo = new RasterizationInfo()
                {
                    CullMode = CullModeFlags.None,
                    FrontFace = FrontFace.Clockwise,
                    PolygonMode = PolygonMode.Fill,
                    LineWidth = 1f
                },
                ColorBlendInfo = new ColorBlendInfo()
                {
                    Attachments = new[]
                    {
                        new PipelineColorBlendAttachmentState
                        {
                            BlendEnable = Vk.True,
                            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit |
                                             ColorComponentFlags.BBit |
                                             ColorComponentFlags.ABit,
                            SrcColorBlendFactor = BlendFactor.SrcAlpha,
                            DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                            ColorBlendOp = BlendOp.Add,
                            SrcAlphaBlendFactor = BlendFactor.One,
                            DstAlphaBlendFactor = BlendFactor.Zero,
                            AlphaBlendOp = BlendOp.Add
                        }
                    }
                },
                DescriptorSets = Array.Empty<Identification>(),
                PushConstantRanges = Array.Empty<PushConstantRange>(),
                VertexAttributeDescriptions = Array.Empty<VertexInputAttributeDescription>(),
                VertexInputBindingDescriptions = Array.Empty<VertexInputBindingDescription>()
            };
        }
    }
}