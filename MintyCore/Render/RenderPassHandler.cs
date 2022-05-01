using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

/// <summary>
///     Handler class for render passes
/// </summary>
public static unsafe class RenderPassHandler
{
    private static readonly Dictionary<Identification, RenderPass> _renderPasses = new();

    private static RenderPass _defaultMainRenderPass;

    /// <summary>
    ///     The main render passed used in rendering
    /// </summary>
    public static RenderPass MainRenderPass { get; private set; }

    /// <summary>
    ///     Get a Render pass
    /// </summary>
    /// <param name="renderPassId"></param>
    /// <returns></returns>
    public static RenderPass GetRenderPass(Identification renderPassId)
    {
        return _renderPasses.TryGetValue(renderPassId, out var renderPass) ? renderPass : MainRenderPass;
    }

    /// <summary>
    ///     Set the main render pass
    /// </summary>
    /// <param name="renderPass"></param>
    public static void SetMainRenderPass(Identification renderPass)
    {
        MainRenderPass = _renderPasses[renderPass];
    }

    /// <summary>
    ///     Set the main render pass
    /// </summary>
    /// <param name="renderPass"></param>
    public static void SetMainRenderPass(RenderPass renderPass)
    {
        MainRenderPass = renderPass;
    }

    internal static void AddRenderPass(Identification id, Span<AttachmentDescription> attachments,
        Span<SubpassDescription> subPasses, Span<SubpassDependency> dependencies,
        RenderPassCreateFlags flags = 0)
    {
        RenderPassCreateInfo createInfo = new()
        {
            SType = StructureType.RenderPassCreateInfo,
            PNext = null,
            Flags = flags,
            AttachmentCount = (uint) attachments.Length,
            PAttachments = (AttachmentDescription*) Unsafe.AsPointer(ref attachments.GetPinnableReference()),
            DependencyCount = (uint) dependencies.Length,
            PDependencies = (SubpassDependency*) Unsafe.AsPointer(ref dependencies.GetPinnableReference()),
            SubpassCount = (uint) subPasses.Length,
            PSubpasses = (SubpassDescription*) Unsafe.AsPointer(ref subPasses.GetPinnableReference())
        };
        VulkanUtils.Assert(VulkanEngine.Vk.CreateRenderPass(VulkanEngine.Device, createInfo,
            VulkanEngine.AllocationCallback, out var renderPass));

        _renderPasses.Add(id, renderPass);
    }

    internal static void CreateMainRenderPass(Format swapchainImageFormat)
    {
        var attachments = stackalloc AttachmentDescription[]
        {
            //color
            new()
            {
                Format = swapchainImageFormat,
                Samples = SampleCountFlags.SampleCount1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr
            },
            //depth
            new()
            {
                Format = Format.D32Sfloat,
                Samples = SampleCountFlags.SampleCount1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.Load,
                StencilStoreOp = AttachmentStoreOp.Store,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
            }
        };

        AttachmentReference colorAttachmentReference = new()
        {
            Attachment = 0u,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        AttachmentReference depthAttachmentReference = new()
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal
        };

        SubpassDescription subpassDescription = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            PInputAttachments = null,
            InputAttachmentCount = 0u,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentReference,
            PDepthStencilAttachment = &depthAttachmentReference
        };

        SubpassDependency subpassDependency = new()
        {
            DstSubpass = 0,
            SrcSubpass = Vk.SubpassExternal,
            SrcStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
            DstStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
            SrcAccessMask = AccessFlags.AccessNoneKhr,
            DstAccessMask = AccessFlags.AccessColorAttachmentWriteBit | AccessFlags.AccessColorAttachmentReadBit
        };

        RenderPassCreateInfo renderPassCreateInfo = new()
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 2,
            PAttachments = attachments,
            SubpassCount = 1,
            PSubpasses = &subpassDescription,
            DependencyCount = 1,
            PDependencies = &subpassDependency
        };

        VulkanUtils.Assert(VulkanEngine.Vk.CreateRenderPass(VulkanEngine.Device, renderPassCreateInfo,
            VulkanEngine.AllocationCallback,
            out _defaultMainRenderPass));
        MainRenderPass = _defaultMainRenderPass;
    }

    internal static void DestroyMainRenderPass()
    {
        VulkanEngine.Vk.DestroyRenderPass(VulkanEngine.Device, _defaultMainRenderPass,
            VulkanEngine.AllocationCallback);

        _defaultMainRenderPass = default;
        MainRenderPass = default;
    }

    internal static void Clear()
    {
        foreach (var renderPass in _renderPasses.Values)
            VulkanEngine.Vk.DestroyRenderPass(VulkanEngine.Device, renderPass, VulkanEngine.AllocationCallback);

        _renderPasses.Clear();
    }

    internal static void RemoveRenderPass(Identification objectId)
    {
        if (_renderPasses.Remove(objectId, out var renderPass))
            VulkanEngine.Vk.DestroyRenderPass(VulkanEngine.Device, renderPass, VulkanEngine.AllocationCallback);
    }
}