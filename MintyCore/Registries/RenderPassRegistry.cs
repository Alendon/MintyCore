using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
/// <see cref="IRegistry"/> for <see cref="RenderPass"/>
/// </summary>
public class RenderPassRegistry : IRegistry
{
    /// <summary />
    public delegate void RegisterDelegate();

    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.RenderPass;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    /// <inheritdoc />
    public void PreRegister()
    {
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
    }

    /// <inheritdoc />
    public void Clear()
    {
        RenderPassHandler.Clear();
        OnRegister = delegate { };
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
    }

    /// <summary />
    public static event RegisterDelegate OnRegister = delegate { };

    /// <summary>
    /// Register a new render pass
    /// </summary>
    /// <param name="modId">Id of the mod registering</param>
    /// <param name="stringIdentifier">String identifier of the content to register</param>
    /// <param name="attachments">Attachments used in the render pass</param>
    /// <param name="subPasses">Sub passes used in the render  pass</param>
    /// <param name="dependencies">Subpass dependencies used in the render pass</param>
    /// <param name="flags">Optional flags for render pass creation</param>
    /// <returns><see cref="Identification"/> of the created render pass</returns>
    public static Identification RegisterRenderPass(ushort modId, string stringIdentifier,
        Span<AttachmentDescription> attachments, Span<SubpassDescription> subPasses,
        Span<SubpassDependency> dependencies, RenderPassCreateFlags flags = 0)
    {
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.RenderPass, stringIdentifier);
        RenderPassHandler.AddRenderPass(id, attachments, subPasses, dependencies, flags);
        return id;
    }
}