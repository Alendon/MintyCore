using System;
using System.IO;
using System.Runtime.InteropServices;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers.Implementations;
using MintyCore.Identifications;
using MintyCore.Registries;
using Silk.NET.Vulkan;

namespace MintyCore.AvaloniaIntegration;

public static class UiRenderObjects
{
    [RegisterShader2("ui_vertex")]
    internal static ShaderInfo2 UiVertexShaderInfo =>
        new(ShaderStageFlags.VertexBit, ReadShaderCode("MintyCore.AvaloniaIntegration.shaders.avalonia_ui.vert.spv"));

    [RegisterShader2("ui_frag")]
    internal static ShaderInfo2 UiFragmentShaderInfo =>
        new(ShaderStageFlags.FragmentBit, ReadShaderCode("MintyCore.AvaloniaIntegration.shaders.avalonia_ui.frag.spv"));

    [RegisterGraphicsPipeline("ui_pipeline")]
    internal static GraphicsPipelineDescription UiPipelineDescription(IVulkanEngine vulkanEngine) =>
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
                DescriptorSetIDs.SampledTexture
            ],
            SampleCount = SampleCountFlags.Count1Bit,
            RasterizationInfo = new RasterizationInfo
            {
                CullMode = CullModeFlags.BackBit,
                FrontFace = FrontFace.Clockwise,
                LineWidth = 1
            },
            DepthStencilInfo = new DepthStencilInfo
            {
                DepthCompareOp = CompareOp.Never
            },
            RenderDescription = new DynamicRenderingDescription([vulkanEngine.SwapchainImageFormat]),
            ColorBlendInfo = new ColorBlendInfo
            {
                Attachments =
                [
                    new()
                    {
                        BlendEnable = Vk.True,
                        SrcColorBlendFactor = BlendFactor.SrcAlpha,
                        DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                        ColorBlendOp = BlendOp.Add,
                        SrcAlphaBlendFactor = BlendFactor.One,
                        DstAlphaBlendFactor = BlendFactor.Zero,
                        AlphaBlendOp = BlendOp.Add,
                        ColorWriteMask = ColorComponentFlags.ABit | ColorComponentFlags.RBit |
                                         ColorComponentFlags.GBit | ColorComponentFlags.BBit
                    }
                ]
            }
        };

    private static ReadOnlySpan<uint> ReadShaderCode(string name)
    {
        var assembly = typeof(UiRenderObjects).Assembly;
        using var vertexShaderStream = assembly.GetManifestResourceStream(name);

        if (vertexShaderStream is null)
        {
            throw new FileNotFoundException($"Could not find embedded resource '{name}'");
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