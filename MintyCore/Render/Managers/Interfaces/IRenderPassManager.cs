using System;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.Managers.Interfaces;

public interface IRenderPassManager
{
    /// <summary>
    ///     The main render passed used in rendering
    /// </summary>
    RenderPass MainRenderPass { get; }

    /// <summary>
    ///     Get a Render pass
    /// </summary>
    /// <param name="renderPassId"></param>
    /// <returns></returns>
    RenderPass GetRenderPass(Identification renderPassId);

    void AddRenderPass(Identification id, RenderPass renderPass);

    unsafe void AddRenderPass(Identification id, Span<AttachmentDescription> attachments,
        SubpassDescriptionInfo[] subPasses, Span<SubpassDependency> dependencies,
        RenderPassCreateFlags flags = 0);

    unsafe void CreateMainRenderPass(Format swapchainImageFormat);
    unsafe void DestroyMainRenderPass();
    unsafe void Clear();
    unsafe void RemoveRenderPass(Identification objectId);
}