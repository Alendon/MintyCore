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
        var pDescriptorSets = stackalloc DescriptorSetLayout[description.DescriptorSets.Length];

        for (var i = 0; i < description.DescriptorSets.Length; i++)
            pDescriptorSets[i] = DescriptorSetHandler.GetDescriptorSetLayout(description.DescriptorSets[i]);

        PipelineLayoutCreateInfo layoutCreateInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            Flags = 0,
            PNext = null,
            PushConstantRangeCount = 0,
            PPushConstantRanges = null,
            PSetLayouts = pDescriptorSets,
            SetLayoutCount = (uint)description.DescriptorSets.Length
        };
        VulkanUtils.Assert(VulkanEngine.Vk.CreatePipelineLayout(VulkanEngine.Device, layoutCreateInfo,
            VulkanEngine.AllocationCallback, out var pipelineLayout));


        var shaderInfos =
            stackalloc PipelineShaderStageCreateInfo[description.Shaders.Length];
        for (var i = 0; i < description.Shaders.Length; i++)
        {
            var shader = ShaderHandler.GetShader(description.Shaders[i]);
            shaderInfos[i] = shader.GetCreateInfo();
        }

        Pipeline pipeline;

        fixed (DynamicState* pDynamicStates = &description.DynamicStates.AsSpan().GetPinnableReference())
        fixed (VertexInputBindingDescription* pVertexBindings =
                   &description.VertexInputBindingDescriptions.AsSpan().GetPinnableReference())
        fixed (VertexInputAttributeDescription* pVertexAttributes =
                   &description.VertexAttributeDescriptions.AsSpan().GetPinnableReference())
        fixed (Rect2D* pScissors = &description.Scissors.AsSpan().GetPinnableReference())
        fixed (Viewport* pViewports = &description.Viewports.AsSpan().GetPinnableReference())
        fixed (PipelineColorBlendAttachmentState* pAttachments =
                   &description.ColorBlendInfo.Attachments.AsSpan().GetPinnableReference())
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
                VertexAttributeDescriptionCount = (uint)description.VertexAttributeDescriptions.Length,
                VertexBindingDescriptionCount = (uint)description.VertexInputBindingDescriptions.Length
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
                ScissorCount = (uint)description.Scissors.Length,
                ViewportCount = (uint)description.Viewports.Length
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
                DepthBoundsTestEnable = description.DepthStencilInfo.DepthBoundsTestEnable ? Vk.True : Vk.False
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
                StageCount = (uint)description.Shaders.Length,
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

    /// <summary>
    ///     Get a pipeline layout
    /// </summary>
    /// <param name="pipelineId"></param>
    /// <returns></returns>
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

    internal static unsafe void RemovePipeline(Identification objectId)
    {
        if (_pipelines.Remove(objectId, out var pipeline))
        {
            VulkanEngine.Vk.DestroyPipeline(VulkanEngine.Device, pipeline, VulkanEngine.AllocationCallback);
        }

        if (_pipelineLayouts.Remove(objectId, out var pipelineLayout))
        {
            VulkanEngine.Vk.DestroyPipelineLayout(VulkanEngine.Device, pipelineLayout, VulkanEngine.AllocationCallback);
        }

        _manuallyAdded.Remove(objectId);
    }
}

/// <summary>
///     Helper struct for pipeline creation
///     Not all values have to be set
/// </summary>
public struct GraphicsPipelineDescription
{
    /// <summary>
    ///     Descriptor sets used in the pipeline
    /// </summary>
    public Identification[] DescriptorSets;

    /// <summary>
    ///     Flags used for pipeline creation
    /// </summary>
    public PipelineCreateFlags Flags;

    /// <summary>
    ///     The subpass of the pipeline
    /// </summary>
    public uint SubPass;

    /// <summary>
    ///     The identification of the <see cref="RenderPass" /> used for the pipeline
    ///     Leave as default for the MainRenderPass
    ///     <seealso cref="GraphicsPipelineCreateInfo.RenderPass" />
    /// </summary>
    public Identification RenderPass;

    /// <summary>
    ///     Shaders used for the pipeline
    /// </summary>
    public Identification[] Shaders;

    /// <summary>
    ///     Dynamic states used in the pipeline (scissor and viewport is recommended in general)
    /// </summary>
    public DynamicState[] DynamicStates;

    /// <summary>
    ///     Base pipeline handle used in the pipeline creation
    /// </summary>
    public Pipeline BasePipelineHandle;

    /// <summary>
    ///     Base pipeline index
    /// </summary>
    public int BasePipelineIndex;

    /// <summary>
    ///     The sample count of the pipeline
    /// </summary>
    public SampleCountFlags SampleCount;

    /// <summary>
    ///     Whether or not Alpha to coverage should be enabled in the pipeline
    /// </summary>
    public bool AlphaToCoverageEnable;

    /// <summary>
    ///     Vertex Input attributes used in the pipeline
    /// </summary>
    public VertexInputAttributeDescription[] VertexAttributeDescriptions;

    /// <summary>
    ///     Vertex Input bindings used in the pipeline
    /// </summary>
    public VertexInputBindingDescription[] VertexInputBindingDescriptions;

    /// <summary>
    ///     Rasterization info for the pipeline creation
    /// </summary>
    public RasterizationInfo RasterizationInfo;

    /// <summary>
    ///     Viewports used in the pipeline
    /// </summary>
    public Viewport[] Viewports;

    /// <summary>
    ///     Scissors used in the pipeline
    /// </summary>
    public Rect2D[] Scissors;

    /// <summary>
    ///     Color blend information
    /// </summary>
    public ColorBlendInfo ColorBlendInfo;

    /// <summary>
    ///     Depth stencil information
    /// </summary>
    public DepthStencilInfo DepthStencilInfo;

    /// <summary>
    ///     Which topology to use for the pipeline
    /// </summary>
    public PrimitiveTopology Topology;

    /// <summary>
    ///     Primitive restart enabled
    /// </summary>
    public bool PrimitiveRestartEnable;
}

/// <summary>
///     Struct which contains information how to handle depth stencils
/// </summary>
public struct DepthStencilInfo
{
    /// <summary>
    ///     Stencil operation for the back
    /// </summary>
    public StencilOpState Back;

    /// <summary>
    ///     Stencil operation for the front
    /// </summary>
    public StencilOpState Front;

    /// <summary>
    ///     How to compare the depth
    /// </summary>
    public CompareOp DepthCompareOp;

    /// <summary>
    ///     The maximum depth bounds to check
    /// </summary>
    public float MaxDepthBounds;

    /// <summary>
    ///     The minimum depth bounds to check
    /// </summary>
    public float MinDepthBounds;

    /// <summary>
    ///     Is depth testing enabled
    /// </summary>
    public bool DepthTestEnable;

    /// <summary>
    ///     is depth writing enabled
    /// </summary>
    public bool DepthWriteEnable;

    /// <summary>
    ///     Is stencil testing enabled
    /// </summary>
    public bool StencilTestEnable;

    /// <summary>
    ///     Is depth bounds test enabled
    /// </summary>
    public bool DepthBoundsTestEnable;
}

/// <summary>
///     Struct containing color blend information for the pipeline creation
/// </summary>
public unsafe struct ColorBlendInfo
{
    /// <summary>
    ///     Fixed array of blend constants
    /// </summary>
    public fixed float BlendConstants[4];

    /// <summary>
    ///     Is the logic operation enabled
    /// </summary>
    public bool LogicOpEnable;

    /// <summary>
    ///     Which logic operation to use for color blending
    /// </summary>
    public LogicOp LogicOp;

    /// <summary>
    ///     Color blend attachments to use
    /// </summary>
    public PipelineColorBlendAttachmentState[] Attachments;
}

/// <summary>
///     Struct containing the rasterization info for the pipeline creation
/// </summary>
public struct RasterizationInfo
{
    /// <summary>
    ///     Which side
    /// </summary>
    public FrontFace FrontFace;

    /// <summary>
    ///     What to cull
    /// </summary>
    public CullModeFlags CullMode;

    /// <summary>
    ///     Line width (only used if chosen a line polygon mode)
    /// </summary>
    public float LineWidth;

    /// <summary>
    ///     How to interpret polygons
    /// </summary>
    public PolygonMode PolygonMode;

    /// <summary>
    ///     Is depth biasing enabled
    /// </summary>
    public bool DepthBiasEnable;

    /// <summary>
    ///     Is depth clamping enabled
    /// </summary>
    public bool DepthClampEnable;

    /// <summary>
    ///     Is rasterizer discard enabled
    /// </summary>
    public bool RasterizerDiscardEnable;

    /// <summary>
    ///     Is depth bias clamping enabled
    /// </summary>
    public float DepthBiasClamp;

    /// <summary>
    ///     Depth bias constant factor
    /// </summary>
    public float DepthBiasConstantFactor;

    /// <summary>
    ///     Depth bias slope factor
    /// </summary>
    public float DepthBiasSlopeFactor;
}