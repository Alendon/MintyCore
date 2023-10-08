using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Render;
using MintyCore.Render.Managers;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Pipeline" />
/// </summary>
[Registry("pipeline")]
[PublicAPI]
public class PipelineRegistry : IRegistry
{
    /// <inheritdoc />
    public void PreUnRegister()
    {
    }
    
    public required IPipelineManager PipelineManager { private get; init; }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        if (Engine.HeadlessModeActive)
            return;
        PipelineManager.RemovePipeline(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Pipelines", LogImportance.Info, "Registry");
        ClearRegistryEvents();
        PipelineManager.Clear();
    }

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
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Pipeline;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => new[]
        {RegistryIDs.Shader, RegistryIDs.RenderPass, RegistryIDs.DescriptorSet /*, RegistryIDs.Texture */};

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    /// <summary>
    /// Register a graphics pipeline
    /// Used by the SourceGenerator to create <see cref="RegisterGraphicsPipelineAttribute"/>
    /// </summary>
    /// <param name="id">Id of the pipeline</param>
    /// <param name="description"></param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterGraphicsPipeline(Identification id, GraphicsPipelineDescription description)
    {
        if (Engine.HeadlessModeActive)
            return;

        PipelineManager.AddGraphicsPipeline(id, description);
    }
}