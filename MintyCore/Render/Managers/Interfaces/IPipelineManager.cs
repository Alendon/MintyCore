using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.Managers.Interfaces;

public interface IPipelineManager
{
    void AddGraphicsPipeline(Identification id, Pipeline pipeline, PipelineLayout pipelineLayout);
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

    void Clear();
    void RemovePipeline(Identification objectId);
}