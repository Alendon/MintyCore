using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Managers.Implementations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;
using Serilog;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Pipeline" />
/// </summary>
[Registry("pipeline", applicableGameType: GameType.Client)]
[PublicAPI]
public class PipelineRegistry(IEngineConfiguration engineConfiguration) : IRegistry
{
    /// <summary/>
    public required IPipelineManager PipelineManager { private get; init; }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        if (engineConfiguration.HeadlessModeActive)
            return;
        PipelineManager.RemovePipeline(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        Log.Information("Clearing Pipelines");
        PipelineManager.Clear();
    }


    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Pipeline;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => new[]
        { RegistryIDs.Shader, RegistryIDs.RenderPass, RegistryIDs.DescriptorSet /*, RegistryIDs.Texture */ };

    /// <summary>
    /// Register a graphics pipeline
    /// Used by the SourceGenerator to create <see cref="RegisterGraphicsPipelineAttribute"/>
    /// </summary>
    /// <param name="id">Id of the pipeline</param>
    /// <param name="description"></param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterGraphicsPipeline(Identification id, GraphicsPipelineDescription description)
    {
        if (engineConfiguration.HeadlessModeActive)
            return;

        PipelineManager.AddGraphicsPipeline(id, description);
    }

    /// <summary>
    ///  Register a existing graphics pipeline
    /// </summary>
    /// <param name="id"> Id of the pipeline</param>
    /// <param name="pipeline"> The pipeline</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterExistingGraphicsPipeline(Identification id, ExistingPipelineDescription pipeline)
    {
        if (engineConfiguration.HeadlessModeActive)
            return;

        PipelineManager.AddGraphicsPipeline(id, pipeline.Pipeline, pipeline.PipelineLayout);
    }
}

/// <summary>
/// Wrapper object for registering a existing pipeline
/// </summary>
/// <param name="Pipeline"> The pipeline</param>
/// <param name="PipelineLayout"> The layout of the pipeline</param>
[PublicAPI]
public record ExistingPipelineDescription(Pipeline Pipeline, PipelineLayout PipelineLayout);