using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     <see cref="IRegistry" /> for <see cref="RenderPass" />
/// </summary>
[Registry("render_pass", applicableGameType: GameType.Client)]
[PublicAPI]
public class RenderPassRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.RenderPass;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    public required IRenderPassManager RenderPassManager { private get; init; }


    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        if (Engine.HeadlessModeActive)
            return;
        RenderPassManager.RemoveRenderPass(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        RenderPassManager.Clear();
    }

    /// <summary>
    /// Register a new render pass
    /// Used by the SourceGenerator to create <see cref="RegisterRenderPassAttribute"/>
    /// </summary>
    /// <param name="id"> <see cref="Identification"/> of the render pass</param>
    /// <param name="info"> <see cref="RenderPassInfo"/> of the render pass</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterRenderPass(Identification id, RenderPassInfo info)
    {
        if (Engine.HeadlessModeActive)
            return;

        RenderPassManager.AddRenderPass(id, info.Attachments, info.SubPasses, info.Dependencies, info.Flags);
    }

    /// <summary>
    /// Register an existing render pass
    /// Used by the SourceGenerator to create <see cref="RegisterRenderPassAttribute"/>
    /// </summary>
    /// <param name="id"> <see cref="Identification"/> of the render pass</param>
    /// <param name="renderPass">RenderPass to register</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterExistingRenderPass(Identification id, RenderPass renderPass)
    {
        if (Engine.HeadlessModeActive)
            return;
        RenderPassManager.AddRenderPass(id, renderPass);
    }
}

/// <summary>
///    Render pass info
/// </summary>
public struct RenderPassInfo
{
    /// <summary>
    /// Attachments used in the render pass
    /// </summary>
    public readonly AttachmentDescription[] Attachments;

    /// <summary>
    /// Sub passes used in the render  pass
    /// </summary>
    public readonly SubpassDescriptionInfo[] SubPasses;

    /// <summary>
    /// Subpass dependencies used in the render pass
    /// </summary>
    public readonly SubpassDependency[] Dependencies;

    /// <summary>
    ///   Optional flags for render pass creation
    /// </summary>
    public readonly RenderPassCreateFlags Flags;

    /// <summary>
    ///   Create a new render pass info
    /// </summary>
    /// <param name="attachments"> Attachments used in the render pass</param>
    /// <param name="subPasses"> Sub passes used in the render  pass</param>
    /// <param name="dependencies"> Subpass dependencies used in the render pass</param>
    /// <param name="flags"> Optional flags for render pass creation</param>
    public RenderPassInfo(AttachmentDescription[] attachments, SubpassDescriptionInfo[] subPasses,
        SubpassDependency[] dependencies, RenderPassCreateFlags flags)
    {
        Attachments = attachments;
        SubPasses = subPasses;
        Dependencies = dependencies;
        Flags = flags;
    }
}

/// <summary>
/// Wrapper struct to hold the information's needed to register a render subpass
/// <see cref="RenderPassCreateInfo.PSubpasses"/>
/// </summary>
[PublicAPI]
public struct SubpassDescriptionInfo
{
    /// <summary>
    /// <see cref="SubpassDescription.Flags"/>
    /// </summary>
    public SubpassDescriptionFlags Flags { get; set; }

    /// <summary>
    /// <see cref="SubpassDescription.PipelineBindPoint"/>
    /// </summary>
    public PipelineBindPoint PipelineBindPoint { get; set; }

    /// <summary>
    /// <see cref="SubpassDescription.PInputAttachments"/>
    /// </summary>
    public AttachmentReference[] InputAttachments { get; set; }

    /// <summary>
    /// <see cref="SubpassDescription.PColorAttachments"/>
    /// </summary>
    public AttachmentReference[] ColorAttachments { get; set; }

    /// <summary>
    /// <see cref="SubpassDescription.PResolveAttachments"/>
    /// </summary>
    public AttachmentReference ResolveAttachment { get; set; }

    /// <summary>
    /// True if a resolve attachment should be used
    /// <see cref="SubpassDescription.Flags"/>
    /// </summary>
    public bool HasResolveAttachment { get; set; }

    /// <summary>
    /// <see cref="SubpassDescription.PDepthStencilAttachment"/>
    /// </summary>
    public AttachmentReference DepthStencilAttachment { get; set; }

    /// <summary>
    /// True if a depth stencil attachment should be used
    /// <see cref="SubpassDescription.PDepthStencilAttachment"/>
    /// </summary>
    public bool HasDepthStencilAttachment { get; set; }

    public SubpassDescriptionInfo()
    {
        Flags = SubpassDescriptionFlags.None;
        PipelineBindPoint = PipelineBindPoint.Graphics;
        InputAttachments = Array.Empty<AttachmentReference>();
        ColorAttachments = Array.Empty<AttachmentReference>();
        ResolveAttachment = default;
        HasResolveAttachment = false;
        DepthStencilAttachment = default;
        PreserveAttachments = Array.Empty<uint>();
        HasDepthStencilAttachment = false;
    }

    /// <summary>
    /// <see cref="SubpassDescription.PPreserveAttachments"/>
    /// </summary>
    public uint[] PreserveAttachments { get; set; }
}