using JetBrains.Annotations;
using MintyCore.Graphics.Managers.Implementations;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Managers;

/// <summary>
///   Manages the creation and deletion of pipelines
/// </summary>
[PublicAPI]
public interface IPipelineManager
{
    /// <summary>
    ///  Add a externally created pipeline to the manager
    /// </summary>
    /// <param name="id"> Id of the pipeline</param>
    /// <param name="pipeline"> The pipeline</param>
    /// <param name="pipelineLayout"> The layout of the pipeline</param>
    void AddGraphicsPipeline(Identification id, Pipeline pipeline, PipelineLayout pipelineLayout);

    /// <summary>
    ///  Add a new pipeline to the manager
    /// </summary>
    /// <param name="id"> Id of the pipeline</param>
    /// <param name="description"> Description of the pipeline</param>
    void AddGraphicsPipeline(Identification id, in GraphicsPipelineDescription description);

    /// <summary>
    ///     Get a pipeline
    /// </summary>
    /// <param name="pipelineId"></param>
    /// <returns></returns>
    Pipeline GetPipeline(Identification pipelineId);

    /// <summary>
    ///     Get a pipeline layout
    /// </summary>
    /// <param name="pipelineId"></param>
    /// <returns></returns>
    PipelineLayout GetPipelineLayout(Identification pipelineId);

    /// <summary>
    ///  Clear all internal data
    /// </summary>
    void Clear();
    
    /// <summary>
    ///  Remove a pipeline
    /// </summary>
    /// <param name="objectId"> Id of the pipeline</param>
    void RemovePipeline(Identification objectId);
}