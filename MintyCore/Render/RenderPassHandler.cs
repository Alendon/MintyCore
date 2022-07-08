using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

/// <summary>
///     Handler class for render passes
/// </summary>
public static unsafe class RenderPassHandler
{
    private static readonly Dictionary<Identification, RenderPass> _renderPasses = new();
    private static readonly HashSet<Identification> _unmanagedRenderPasses = new();

    /// <summary>
    ///     The main render passed used in rendering
    /// </summary>
    public static RenderPass MainRenderPass { get; internal set; }

    /// <summary>
    ///     Get a Render pass
    /// </summary>
    /// <param name="renderPassId"></param>
    /// <returns></returns>
    public static RenderPass GetRenderPass(Identification renderPassId)
    {
        return _renderPasses.TryGetValue(renderPassId, out var renderPass) ? renderPass : MainRenderPass;
    }

    internal static void AddRenderPass(Identification id, RenderPass renderPass)
    {
        _renderPasses.Add(id, renderPass);
        _unmanagedRenderPasses.Add(id);
    }

    internal static void AddRenderPass(Identification id, Span<AttachmentDescription> attachments,
        SubpassDescriptionInfo[] subPasses, Span<SubpassDependency> dependencies,
        RenderPassCreateFlags flags = 0)
    {
        Stack<GCHandle> arrayHandles = new();
        Span<AttachmentReference> depthStencilAttachments = stackalloc AttachmentReference[subPasses.Length];
        Span<AttachmentReference> resolveAttachments = stackalloc AttachmentReference[subPasses.Length];

        Span<SubpassDescription> subpassDescriptions = stackalloc SubpassDescription[subPasses.Length];
        for (var i = 0; i < subPasses.Length; i++)
        {
            arrayHandles.Push(GCHandle.Alloc(subPasses[i].ColorAttachments, GCHandleType.Pinned));
            arrayHandles.Push(GCHandle.Alloc(subPasses[i].InputAttachments, GCHandleType.Pinned));
            arrayHandles.Push(GCHandle.Alloc(subPasses[i].PreserveAttachments, GCHandleType.Pinned));

            depthStencilAttachments[i] = subPasses[i].DepthStencilAttachment;
            resolveAttachments[i] = subPasses[i].ResolveAttachment;

            subpassDescriptions[i] = new SubpassDescription
            {
                Flags = subPasses[i].Flags,
                PipelineBindPoint = subPasses[i].PipelineBindPoint,
                PDepthStencilAttachment = subPasses[i].HasDepthStencilAttachment
                    ? (AttachmentReference*) Unsafe.AsPointer(ref depthStencilAttachments[i])
                    : null,
                PColorAttachments = subPasses[i].ColorAttachments.Length > 0
                    ? (AttachmentReference*) Unsafe.AsPointer(ref subPasses[i].ColorAttachments[0])
                    : null,
                ColorAttachmentCount = (uint) subPasses[i].ColorAttachments.Length,
                PInputAttachments = subPasses[i].InputAttachments.Length > 0
                    ? (AttachmentReference*) Unsafe.AsPointer(ref subPasses[i].InputAttachments[0])
                    : null,
                InputAttachmentCount = (uint) subPasses[i].InputAttachments.Length,
                PResolveAttachments = subPasses[i].HasResolveAttachment
                    ? (AttachmentReference*) Unsafe.AsPointer(ref resolveAttachments[i])
                    : null,
                PPreserveAttachments = subPasses[i].PreserveAttachments.Length > 0
                    ? (uint*) Unsafe.AsPointer(ref subPasses[i].PreserveAttachments[0])
                    : null,
                PreserveAttachmentCount = (uint) subPasses[i].PreserveAttachments.Length
            };
        }

        fixed (AttachmentDescription* attachmentPtr = &attachments[0])
        fixed (SubpassDependency* dependencyPtr = &dependencies[0])
        {
            RenderPassCreateInfo createInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                PNext = null,
                Flags = flags,
                AttachmentCount = (uint) attachments.Length,
                PAttachments = attachmentPtr,
                DependencyCount = (uint) dependencies.Length,
                PDependencies = dependencyPtr,
                SubpassCount = (uint) subPasses.Length,
                PSubpasses = (SubpassDescription*) Unsafe.AsPointer(ref subpassDescriptions[0])
            };
            VulkanUtils.Assert(VulkanEngine.Vk.CreateRenderPass(VulkanEngine.Device, createInfo,
                VulkanEngine.AllocationCallback, out var renderPass));

            _renderPasses.Add(id, renderPass);
        }

        while (arrayHandles.TryPop(out var handle)) handle.Free();
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
                LoadOp = AttachmentLoadOp.Load,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.PresentSrcKhr,
                FinalLayout = ImageLayout.PresentSrcKhr
            },
            //depth
            new()
            {
                Format = Format.D32Sfloat,
                Samples = SampleCountFlags.SampleCount1Bit,
                LoadOp = AttachmentLoadOp.Load,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.Load,
                StencilStoreOp = AttachmentStoreOp.Store,
                InitialLayout = ImageLayout.DepthStencilAttachmentOptimal,
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
            out var renderPass));
        MainRenderPass = renderPass;
    }

    internal static void DestroyMainRenderPass()
    {
        VulkanEngine.Vk.DestroyRenderPass(VulkanEngine.Device, MainRenderPass,
            VulkanEngine.AllocationCallback);

        MainRenderPass = default;
    }

    internal static void Clear()
    {
        foreach (var (id, renderPass) in _renderPasses)
            if (!_unmanagedRenderPasses.Remove(id))
                VulkanEngine.Vk.DestroyRenderPass(VulkanEngine.Device, renderPass, VulkanEngine.AllocationCallback);

        _renderPasses.Clear();
    }

    internal static void RemoveRenderPass(Identification objectId)
    {
        if (_renderPasses.Remove(objectId, out var renderPass)
            && !_unmanagedRenderPasses.Remove(objectId))
            VulkanEngine.Vk.DestroyRenderPass(VulkanEngine.Device, renderPass, VulkanEngine.AllocationCallback);
    }
}