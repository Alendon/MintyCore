using System;
using System.IO;
using System.Runtime.InteropServices;
using MintyCore.Graphics.Managers.Implementations;
using MintyCore.Identifications;
using MintyCore.Registries;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

internal static class RenderObjects
{
    [RegisterShader2("ui_vertex")]
    internal static ShaderInfo2 UiVertexShaderInfo =>
        new(ShaderStageFlags.VertexBit, ReadShaderCode("MintyCore.UI.shaders.ui.vert.spv"));

    [RegisterShader2("ui_frag")]
    internal static ShaderInfo2 UiFragmentShaderInfo =>
        new(ShaderStageFlags.FragmentBit, ReadShaderCode("MintyCore.UI.shaders.ui.frag.spv"));

    [RegisterDescriptorSet("ui_transform_buffer")]
    internal static DescriptorSetInfo UiTransformBufferInfo => new()
    {
        Bindings =
        [
            new DescriptorSetLayoutBinding()
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                StageFlags = ShaderStageFlags.VertexBit
            }
        ],
        DescriptorSetsPerPool = 32
    };

    [RegisterGraphicsPipeline("ui_pipeline")]
    internal static GraphicsPipelineDescription UiPipelineDescription =>
        new()
        {
            Scissors = new Rect2D[1],
            Viewports = new Viewport[1],
            DynamicStates = [DynamicState.Scissor, DynamicState.Viewport],
            Topology = PrimitiveTopology.TriangleList,
            Shaders =
            [
                ShaderIDs.UiFrag,
                ShaderIDs.UiVertex
            ],
            DescriptorSets =
            [
                DescriptorSetIDs.SampledTexture,
                DescriptorSetIDs.UiTransformBuffer
            ],
            SampleCount = SampleCountFlags.Count1Bit,
            RasterizationInfo = new RasterizationInfo()
            {
                CullMode = CullModeFlags.BackBit,
                FrontFace = FrontFace.Clockwise,
                LineWidth = 1
            },
            DepthStencilInfo = new DepthStencilInfo()
            {
                DepthCompareOp = CompareOp.Never
            },
            RenderDescription = new DynamicRenderingDescription([Format.R8G8B8A8Unorm]),
            ColorBlendInfo = new ColorBlendInfo()
            {
                Attachments =
                [
                    new()
                    {
                        BlendEnable = Vk.True,
                        SrcColorBlendFactor = BlendFactor.SrcAlpha,
                        DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                        ColorBlendOp = BlendOp.Add,
                        SrcAlphaBlendFactor = BlendFactor.SrcAlpha,
                        DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
                        AlphaBlendOp = BlendOp.Add,
                        ColorWriteMask = ColorComponentFlags.ABit | ColorComponentFlags.RBit |
                                         ColorComponentFlags.GBit | ColorComponentFlags.BBit
                    }
                ]
            },
            PushConstantRanges =
            [
                new()
                {
                    StageFlags = ShaderStageFlags.VertexBit,
                    Offset = 0,
                    Size = (uint) Marshal.SizeOf<RectangleRenderData>()
                }
            ]
        };

    private static ReadOnlySpan<uint> ReadShaderCode(string name)
    {
        var assembly = typeof(RenderObjects).Assembly;
        using var vertexShaderStream = assembly.GetManifestResourceStream(name);

        if (vertexShaderStream is null)
        {
            throw new FileNotFoundException("Could not find embedded resource 'MintyCore.UI.shaders.ui.vert.spv'");
        }

        if (vertexShaderStream.Length % 4 != 0)
        {
            throw new InvalidDataException(
                "Invalid vertex shader length. A spir-v file must be a multiple of 4 bytes long.");
        }

        //read the stream into a byte array
        var vertexShaderCode = new byte[vertexShaderStream.Length];
        vertexShaderStream.ReadExactly(vertexShaderCode);
        return MemoryMarshal.Cast<byte, uint>(vertexShaderCode);
    }
}