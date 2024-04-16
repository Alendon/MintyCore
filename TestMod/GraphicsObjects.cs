using System;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers.Implementations;
using MintyCore.Registries;
using Silk.NET.Vulkan;
using ShaderIDs = TestMod.Identifications.ShaderIDs;

namespace TestMod;

public static class GraphicsObjects
{
    [RegisterShader("triangle_vert", "triangle_vert.spv")]
    public static ShaderInfo TriangleVert => new(ShaderStageFlags.VertexBit);

    [RegisterShader("triangle_frag", "triangle_frag.spv")]
    public static ShaderInfo TriangleFrag => new(ShaderStageFlags.FragmentBit);

    [RegisterGraphicsPipeline("triangle")]
    public static GraphicsPipelineDescription GetTrianglePipeline(IVulkanEngine vulkanEngine)
    {
        return new GraphicsPipelineDescription
        {
            Flags = PipelineCreateFlags.None,
            RenderDescription = new DynamicRenderingDescription([vulkanEngine.SwapchainImageFormat]),
            Scissors =
            [
                new()
                {
                    Extent = vulkanEngine.SwapchainExtent,
                    Offset = new Offset2D(0, 0)
                }
            ],
            Viewports =
            [
                new()
                {
                    Height = vulkanEngine.SwapchainExtent.Height,
                    Width = vulkanEngine.SwapchainExtent.Width,
                    MaxDepth = 1
                }
            ],
            Shaders =
            [
                ShaderIDs.TriangleVert,
                ShaderIDs.TriangleFrag
            ],
            Topology = PrimitiveTopology.TriangleList,
            SampleCount = SampleCountFlags.Count1Bit,
            DynamicStates =
            [
                DynamicState.Scissor,
                DynamicState.Viewport
            ],
            RasterizationInfo = new RasterizationInfo
            {
                CullMode = CullModeFlags.None,
                FrontFace = FrontFace.Clockwise,
                PolygonMode = PolygonMode.Fill,
                LineWidth = 1f
            },
            ColorBlendInfo = new ColorBlendInfo
            {
                Attachments =
                [
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
                ]
            },
            DescriptorSets =
            [
                Identifications.DescriptorSetIDs.BufferBind
            ],
            PushConstantRanges = Array.Empty<PushConstantRange>(),
            VertexAttributeDescriptions = Array.Empty<VertexInputAttributeDescription>(),
            VertexInputBindingDescriptions = Array.Empty<VertexInputBindingDescription>()
        };
    }
}