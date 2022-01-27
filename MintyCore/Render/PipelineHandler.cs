using System;
using System.Collections.Generic;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

/// <summary>
///     Class to handle <see cref="Pipeline" />
/// </summary>
public static class PipelineHandler
{
    private static readonly Dictionary<Identification, Pipeline> _pipelines = new();
    private static readonly Dictionary<Identification, PipelineLayout> _pipelineLayouts = new();
    private static readonly HashSet<Identification> _manuallyAdded = new();

    internal static void AddGraphicsPipeline(Identification id, Pipeline pipeline, PipelineLayout pipelineLayout)
    {
        _pipelines.Add(id, pipeline);
        _pipelineLayouts.Add(id, pipelineLayout);
        _manuallyAdded.Add(id);
    }

    internal static unsafe void AddGraphicsPipeline(Identification id, in GraphicsPipelineDescription description)
    {
        var pDescriptorSets = stackalloc DescriptorSetLayout[description.descriptorSets.Length];

        for (var i = 0; i < description.descriptorSets.Length; i++)
            pDescriptorSets[i] = DescriptorSetHandler.GetDescriptorSetLayout(description.descriptorSets[i]);

        PipelineLayoutCreateInfo layoutCreateInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            Flags = 0,
            PNext = null,
            PushConstantRangeCount = 0,
            PPushConstantRanges = null,
            PSetLayouts = pDescriptorSets,
            SetLayoutCount = (uint)description.descriptorSets.Length
        };
        VulkanUtils.Assert(VulkanEngine.Vk.CreatePipelineLayout(VulkanEngine.Device, layoutCreateInfo,
            VulkanEngine.AllocationCallback, out var pipelineLayout));


        var shaderInfos =
            stackalloc PipelineShaderStageCreateInfo[description.shaders.Length];
        for (var i = 0; i < description.shaders.Length; i++)
        {
            var shader = ShaderHandler.GetShader(description.shaders[i]);
            shaderInfos[i] = shader.GetCreateInfo();
        }

        Pipeline pipeline;

        fixed (DynamicState* pDynamicStates = &description.DynamicStates.GetPinnableReference())
        fixed (VertexInputBindingDescription* pVertexBindings =
                   &description.vertexINputBindingDescriptions.GetPinnableReference())
        fixed (VertexInputAttributeDescription* pVertexAttributes =
                   &description.vertexAttributeDescriptions.GetPinnableReference())
        fixed (Rect2D* pScissors = &description.scissors.GetPinnableReference())
        fixed (Viewport* pViewports = &description.viewports.GetPinnableReference())
        fixed (PipelineColorBlendAttachmentState* pAttachments =
                   &description.ColorBlendInfo.Attachments.GetPinnableReference())
        {
            PipelineDynamicStateCreateInfo dynamicStateCreateInfo = new()
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                Flags = 0,
                PNext = null,
                DynamicStateCount = (uint)description.DynamicStates.Length,
                PDynamicStates = pDynamicStates
            };

            PipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new()
            {
                Flags = 0,
                PNext = null,
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                RasterizationSamples = description.SampleCount,
                AlphaToCoverageEnable = description.AlphaToCoverageEnable
            };

            PipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                PNext = null,
                Flags = 0,
                PVertexAttributeDescriptions = pVertexAttributes,
                PVertexBindingDescriptions = pVertexBindings,
                VertexAttributeDescriptionCount = (uint)description.vertexAttributeDescriptions.Length,
                VertexBindingDescriptionCount = (uint)description.vertexINputBindingDescriptions.Length
            };

            PipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                Flags = 0,
                PNext = null,
                CullMode = description.RasterizationInfo.CullMode,
                FrontFace = description.RasterizationInfo.FrontFace,
                LineWidth = description.RasterizationInfo.LineWidth,
                PolygonMode = description.RasterizationInfo.PolygonMode,
                DepthBiasClamp = description.RasterizationInfo.DepthBiasClamp,
                DepthBiasConstantFactor = description.RasterizationInfo.DepthBiasConstantFactor,
                DepthBiasSlopeFactor = description.RasterizationInfo.DepthBiasSlopeFactor,
                DepthBiasEnable = description.RasterizationInfo.DepthBiasEnable ? Vk.True : Vk.False,
                DepthClampEnable = description.RasterizationInfo.DepthClampEnable ? Vk.True : Vk.False,
                RasterizerDiscardEnable = description.RasterizationInfo.RasterizerDiscardEnable ? Vk.True : Vk.False
            };

            PipelineViewportStateCreateInfo viewportStateCreateInfo = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                Flags = 0,
                PNext = null,
                PScissors = pScissors,
                PViewports = pViewports,
                ScissorCount = (uint)description.scissors.Length,
                ViewportCount = (uint)description.viewports.Length
            };

            PipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                PNext = null,
                Flags = 0,
                LogicOpEnable = description.ColorBlendInfo.LogicOpEnable ? Vk.True : Vk.False,
                LogicOp = description.ColorBlendInfo.LogicOp,
                AttachmentCount = (uint)description.ColorBlendInfo.Attachments.Length,
                PAttachments = pAttachments
            };
            colorBlendStateCreateInfo.BlendConstants[0] = description.ColorBlendInfo.BlendConstants[0];
            colorBlendStateCreateInfo.BlendConstants[1] = description.ColorBlendInfo.BlendConstants[1];
            colorBlendStateCreateInfo.BlendConstants[2] = description.ColorBlendInfo.BlendConstants[2];
            colorBlendStateCreateInfo.BlendConstants[3] = description.ColorBlendInfo.BlendConstants[3];

            PipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo = new()
            {
                SType = StructureType.PipelineDepthStencilStateCreateInfo,
                PNext = null,
                Flags = 0,
                Back = description.DepthStencilInfo.Back,
                Front = description.DepthStencilInfo.Front,
                DepthCompareOp = description.DepthStencilInfo.DepthCompareOp,
                MaxDepthBounds = description.DepthStencilInfo.MaxDepthBounds,
                MinDepthBounds = description.DepthStencilInfo.MinDepthBounds,
                DepthTestEnable = description.DepthStencilInfo.DepthTestEnable ? Vk.True : Vk.False,
                DepthWriteEnable = description.DepthStencilInfo.DepthWriteEnable ? Vk.True : Vk.False,
                StencilTestEnable = description.DepthStencilInfo.StencilTestEnable ? Vk.True : Vk.False,
                DepthBoundsTestEnable = description.DepthStencilInfo.DepthBoundsTestEnable ? Vk.True : Vk.False,
            };

            PipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Flags = 0,
                PNext = null,
                Topology = description.Topology,
                PrimitiveRestartEnable = description.PrimitiveRestartEnable ? Vk.True : Vk.False
            };

            GraphicsPipelineCreateInfo createInfo = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                Flags = description.Flags,
                PNext = null,
                Layout = pipelineLayout,
                Subpass = description.SubPass,
                RenderPass = RenderPassHandler.GetRenderPass(description.RenderPass),
                StageCount = (uint)description.shaders.Length,
                PStages = shaderInfos,
                BasePipelineHandle = description.BasePipelineHandle,
                BasePipelineIndex = description.BasePipelineIndex,
                PDynamicState = &dynamicStateCreateInfo,
                PMultisampleState = &multisampleStateCreateInfo,
                PVertexInputState = &vertexInputStateCreateInfo,
                PRasterizationState = &rasterizationStateCreateInfo,
                PTessellationState = null,
                PViewportState = &viewportStateCreateInfo,
                PColorBlendState = &colorBlendStateCreateInfo,
                PDepthStencilState = &depthStencilStateCreateInfo,
                PInputAssemblyState = &inputAssemblyStateCreateInfo
            };

            VulkanUtils.Assert(VulkanEngine.Vk.CreateGraphicsPipelines(VulkanEngine.Device, default, 1, createInfo,
                VulkanEngine.AllocationCallback, out pipeline));
        }

        _pipelines.Add(id, pipeline);
        _pipelineLayouts.Add(id, pipelineLayout);
    }

    /// <summary>
    ///     Get a pipeline
    /// </summary>
    /// <param name="pipelineId"></param>
    /// <returns></returns>
    public static Pipeline GetPipeline(Identification pipelineId)
    {
        return _pipelines[pipelineId];
    }

    public static PipelineLayout GetPipelineLayout(Identification pipelineId)
    {
        return _pipelineLayouts[pipelineId];
    }

    internal static unsafe void Clear()
    {
        foreach (var (id, pipeline) in _pipelines)
            if (!_manuallyAdded.Contains(id))
                VulkanEngine.Vk.DestroyPipeline(VulkanEngine.Device, pipeline, VulkanEngine.AllocationCallback);

        foreach (var (id, pipelineLayout) in _pipelineLayouts)
            if (!_manuallyAdded.Contains(id))
                VulkanEngine.Vk.DestroyPipelineLayout(VulkanEngine.Device, pipelineLayout,
                    VulkanEngine.AllocationCallback);

        _pipelines.Clear();
        _pipelineLayouts.Clear();
        _manuallyAdded.Clear();
    }
}

public ref struct GraphicsPipelineDescription
{
    public Identification[] descriptorSets;
    public PipelineCreateFlags Flags;
    public uint SubPass;

    /// <summary>
    /// The identification of the <see cref="RenderPass"/> used for the pipeline
    /// Leave as default for the MainRenderPass
    /// <seealso cref="GraphicsPipelineCreateInfo.RenderPass"/>
    /// </summary>
    public Identification RenderPass;

    public Identification[] shaders;
    public ReadOnlySpan<DynamicState> DynamicStates;
    public Pipeline BasePipelineHandle;
    public int BasePipelineIndex;
    public SampleCountFlags SampleCount;
    public bool AlphaToCoverageEnable;
    public ReadOnlySpan<VertexInputAttributeDescription> vertexAttributeDescriptions;
    public ReadOnlySpan<VertexInputBindingDescription> vertexINputBindingDescriptions;

    public RasterizationInfo RasterizationInfo;

    public ReadOnlySpan<Viewport> viewports;
    public ReadOnlySpan<Rect2D> scissors;

    public ColorBlendInfo ColorBlendInfo;
    public DepthStencilInfo DepthStencilInfo;
    public PrimitiveTopology Topology;
    public bool PrimitiveRestartEnable;
}

public struct DepthStencilInfo
{
    public StencilOpState Back;
    public StencilOpState Front;
    public CompareOp DepthCompareOp;
    public float MaxDepthBounds;
    public float MinDepthBounds;
    public bool DepthTestEnable;
    public bool DepthWriteEnable;
    public bool StencilTestEnable;
    public bool DepthBoundsTestEnable;
}

public unsafe ref struct ColorBlendInfo
{
    public fixed float BlendConstants[4];
    public bool LogicOpEnable;
    public LogicOp LogicOp;
    public ReadOnlySpan<PipelineColorBlendAttachmentState> Attachments;
}

public struct RasterizationInfo
{
    public FrontFace FrontFace;
    public CullModeFlags CullMode;
    public float LineWidth;
    public PolygonMode PolygonMode;
    public bool DepthBiasEnable;
    public bool DepthClampEnable;
    public bool RasterizerDiscardEnable;
    public float DepthBiasClamp;
    public float DepthBiasConstantFactor;
    public float DepthBiasSlopeFactor;
}