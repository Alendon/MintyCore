using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     <see cref="IRegistry" /> for <see cref="RenderPass" />
/// </summary>
public class RenderPassRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.RenderPass;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    /// <inheritdoc />
    public void PreRegister()
    {
        OnPreRegister();
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
        OnPostRegister();
    }

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        RenderPassHandler.RemoveRenderPass(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <inheritdoc />
    public void Clear()
    {
        RenderPassHandler.Clear();
        ClearRegistryEvents();
    }

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };


    /// <summary>
    ///     Register a new render pass
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId">Id of the mod registering</param>
    /// <param name="stringIdentifier">String identifier of the content to register</param>
    /// <param name="attachments">Attachments used in the render pass</param>
    /// <param name="subPasses">Sub passes used in the render  pass</param>
    /// <param name="dependencies">Subpass dependencies used in the render pass</param>
    /// <param name="flags">Optional flags for render pass creation</param>
    /// <returns><see cref="Identification" /> of the created render pass</returns>
    public static Identification RegisterRenderPass(ushort modId, string stringIdentifier,
        Span<AttachmentDescription> attachments, Span<SubpassDescription> subPasses,
        Span<SubpassDependency> dependencies, RenderPassCreateFlags flags = 0)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.RenderPass, stringIdentifier);
        RenderPassHandler.AddRenderPass(id, attachments, subPasses, dependencies, flags);
        return id;
    }
}