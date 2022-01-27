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
    /// <summary />
    public delegate void RegisterDelegate();

    /// <inheritdoc />
    public void PreRegister()
    {
    }

    /// <inheritdoc />
    public void Register()
    {
        Logger.WriteLog("Registering Pipelines", LogImportance.INFO, "Registry");
        OnRegister.Invoke();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
    }

    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Pipelines", LogImportance.INFO, "Registry");
        OnRegister = delegate { };
        PipelineHandler.Clear();
    }
        
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
    }

    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Pipeline;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => new[]
        { RegistryIDs.Shader, RegistryIDs.RenderPass, RegistryIDs.DescriptorSet /*, RegistryIDs.Texture */ };

    /// <summary />
    public static event RegisterDelegate OnRegister = delegate { };

    /// <summary>
    ///     Register a graphics <see cref="Pipeline" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Pipeline" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Pipeline" /></param>
    /// <param name="pipelineDescription">The <see cref="GraphicsPipelineDescription" /> for the pipeline to create</param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="Pipeline" /></returns>
    public static Identification RegisterGraphicsPipeline(ushort modId, string stringIdentifier,
        in GraphicsPipelineDescription pipelineDescription)
    {
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Pipeline, stringIdentifier);
        PipelineHandler.AddGraphicsPipeline(id, pipelineDescription);
        return id;
    }

    /// <summary>
    /// Register a created graphics <see cref="Pipeline"/>
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Pipeline" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Pipeline" /></param>
    /// <param name="pipeline">The created <see cref="Pipeline"/></param>
    /// <param name="pipelineLayout">The created <see cref="PipelineLayout"/></param>
    /// <returns>Generated <see cref="Identification"/> for <see cref="Pipeline"/></returns>
    public static Identification RegisterGraphicsPipeline(ushort modId, string stringIdentifier, Pipeline pipeline,
        PipelineLayout pipelineLayout)
    {
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Pipeline, stringIdentifier);
        PipelineHandler.AddGraphicsPipeline(id, pipeline, pipelineLayout);
        return id;
    }
}