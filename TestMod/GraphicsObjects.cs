using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Render.Managers;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using RenderPassIDs = TestMod.Identifications.RenderPassIDs;
using ShaderIDs = TestMod.Identifications.ShaderIDs;

namespace TestMod;

public static class GraphicsObjects
{
    [RegisterShader("triangle_vert", "triangle_vert.spv")]
    public static ShaderInfo TriangleVert => new(ShaderStageFlags.VertexBit);

    [RegisterShader("triangle_frag", "triangle_frag.spv")]
    public static ShaderInfo TriangleFrag => new(ShaderStageFlags.FragmentBit);

    [RegisterShader("background_vert", "background_vert.spv")]
    public static ShaderInfo BackgroundVert => new(ShaderStageFlags.VertexBit);

    [RegisterShader("background_frag", "background_frag.spv")]
    public static ShaderInfo BackgroundFrag => new(ShaderStageFlags.FragmentBit);

    [RegisterShader("ui_vert", "ui_vert.spv")]
    public static ShaderInfo UiVert => new(ShaderStageFlags.VertexBit);

    [RegisterShader("ui_frag", "ui_frag.spv")]
    public static ShaderInfo UiFrag => new(ShaderStageFlags.FragmentBit);

    [RegisterGraphicsPipeline("triangle")]
    public static GraphicsPipelineDescription GetTrianglePipeline(IVulkanEngine vulkanEngine)
    {
        return new GraphicsPipelineDescription()
        {
            Flags = PipelineCreateFlags.None,
            RenderPass = RenderPassIDs.Main,
            Scissors = new Rect2D[]
            {
                new()
                {
                    Extent = vulkanEngine.SwapchainExtent,
                    Offset = new Offset2D(0, 0)
                }
            },
            Viewports = new Viewport[]
            {
                new()
                {
                    Height = vulkanEngine.SwapchainExtent.Height,
                    Width = vulkanEngine.SwapchainExtent.Width,
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

    [RegisterGraphicsPipeline("background")]
    public static GraphicsPipelineDescription GetBackgroundPipeline(IVulkanEngine vulkanEngine)
    {
        return new GraphicsPipelineDescription()
        {
            Flags = PipelineCreateFlags.None,
            RenderPass = RenderPassIDs.Main,
            Scissors = new Rect2D[]
            {
                new()
                {
                    Extent = vulkanEngine.SwapchainExtent,
                    Offset = new Offset2D(0, 0)
                }
            },
            Viewports = new Viewport[]
            {
                new()
                {
                    Height = vulkanEngine.SwapchainExtent.Height,
                    Width = vulkanEngine.SwapchainExtent.Width,
                    MaxDepth = 1
                }
            },
            Shaders = new[]
            {
                ShaderIDs.BackgroundVert,
                ShaderIDs.BackgroundFrag
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

    [RegisterGraphicsPipeline("fill_ui")]
    public static GraphicsPipelineDescription GetFillUiPipeline(IVulkanEngine vulkanEngine)
    {
        return new GraphicsPipelineDescription()
        {
            Flags = PipelineCreateFlags.None,
            RenderPass = RenderPassIDs.Main,
            Scissors = new Rect2D[]
            {
                new()
                {
                    Extent = vulkanEngine.SwapchainExtent,
                    Offset = new Offset2D(0, 0)
                }
            },
            Viewports = new Viewport[]
            {
                new()
                {
                    Height = vulkanEngine.SwapchainExtent.Height,
                    Width = vulkanEngine.SwapchainExtent.Width,
                    MaxDepth = 1
                }
            },
            Shaders = new[]
            {
                ShaderIDs.UiVert,
                ShaderIDs.UiFrag
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
                        SrcColorBlendFactor = BlendFactor.One,
                        DstColorBlendFactor = BlendFactor.Zero,
                        ColorBlendOp = BlendOp.Add,
                        SrcAlphaBlendFactor = BlendFactor.One,
                        DstAlphaBlendFactor = BlendFactor.Zero,
                        AlphaBlendOp = BlendOp.Add
                    }
                }
            },
            DescriptorSets =
            [
                DescriptorSetIDs.SampledTexture
            ],
            PushConstantRanges = Array.Empty<PushConstantRange>(),
            VertexAttributeDescriptions = Array.Empty<VertexInputAttributeDescription>(),
            VertexInputBindingDescriptions = Array.Empty<VertexInputBindingDescription>()
        };
    }
}