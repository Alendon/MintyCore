using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Pipeline" />
/// </summary>
public class PipelineRegistry : IRegistry
{
    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Pipelines", LogImportance.INFO, "Registry");
        ClearRegistryEvents();
        PipelineHandler.Clear();
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
        { RegistryIDs.Shader, RegistryIDs.RenderPass, RegistryIDs.DescriptorSet /*, RegistryIDs.Texture */ };

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    /// <summary>
    ///     Register a graphics <see cref="Pipeline" />
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Pipeline" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Pipeline" /></param>
    /// <param name="pipelineDescription">The <see cref="GraphicsPipelineDescription" /> for the pipeline to create</param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="Pipeline" /></returns>
    public static Identification RegisterGraphicsPipeline(ushort modId, string stringIdentifier,
        in GraphicsPipelineDescription pipelineDescription)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Pipeline, stringIdentifier);
        PipelineHandler.AddGraphicsPipeline(id, pipelineDescription);
        return id;
    }

    /// <summary>
    ///     Register a created graphics <see cref="Pipeline" />
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Pipeline" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Pipeline" /></param>
    /// <param name="pipeline">The created <see cref="Pipeline" /></param>
    /// <param name="pipelineLayout">The created <see cref="PipelineLayout" /></param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="Pipeline" /></returns>
    public static Identification RegisterGraphicsPipeline(ushort modId, string stringIdentifier, Pipeline pipeline,
        PipelineLayout pipelineLayout)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Pipeline, stringIdentifier);
        PipelineHandler.AddGraphicsPipeline(id, pipeline, pipelineLayout);
        return id;
    }
}