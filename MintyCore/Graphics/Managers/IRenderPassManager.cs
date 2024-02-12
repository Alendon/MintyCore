using System;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Managers;

/// <summary>
///   Manages the creation and deletion of render passes
/// </summary>
public interface IRenderPassManager
{
    /// <summary>
    ///     Get a Render pass
    /// </summary>
    /// <param name="renderPassId"></param>
    /// <returns></returns>
    RenderPass GetRenderPass(Identification renderPassId);

    /// <summary>
    ///   Add a externally created render pass to the manager
    /// </summary>
    /// <param name="id"> Id of the render pass</param>
    /// <param name="renderPass"> The render pass</param>
    void AddRenderPass(Identification id, RenderPass renderPass);

    /// <summary>
    /// Adds a new render pass to the manager.
    /// </summary>
    /// <param name="id">The unique identifier for the render pass.</param>
    /// <param name="attachments">A span of attachment descriptions that describe the attachments used by the render pass.</param>
    /// <param name="subPasses">An array of subpass description info that describe the subpasses used by the render pass.</param>
    /// <param name="dependencies">A span of subpass dependencies that describe the dependencies between the subpasses.</param>
    /// <param name="flags">Optional flags that modify the behavior of the render pass creation. Default is 0.</param>
    void AddRenderPass(Identification id, Span<AttachmentDescription> attachments,
        SubpassDescriptionInfo[] subPasses, Span<SubpassDependency> dependencies,
        RenderPassCreateFlags flags = 0);

    /// <summary>
    ///  Clear all internal data
    /// </summary>
    void Clear();
    
    /// <summary>
    ///  Remove a render pass from the manager
    /// </summary>
    void RemoveRenderPass(Identification objectId);
}