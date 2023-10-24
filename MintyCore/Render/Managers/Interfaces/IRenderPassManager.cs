using System;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.Managers.Interfaces;

public interface IRenderPassManager
{
    /// <summary>
    ///     Get a Render pass
    /// </summary>
    /// <param name="renderPassId"></param>
    /// <returns></returns>
    RenderPass GetRenderPass(Identification renderPassId);

    void AddRenderPass(Identification id, RenderPass renderPass);

    void AddRenderPass(Identification id, Span<AttachmentDescription> attachments,
        SubpassDescriptionInfo[] subPasses, Span<SubpassDependency> dependencies,
        RenderPassCreateFlags flags = 0);

    void Clear();
    void RemoveRenderPass(Identification objectId);
}