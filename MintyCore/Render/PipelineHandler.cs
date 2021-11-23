using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.Render
{
    /// <summary>
    ///     Class to handle <see cref="Pipeline" />
    /// </summary>
    public static class PipelineHandler
    {
        private static readonly Dictionary<Identification, Pipeline> _pipelines = new();

        internal static void AddGraphicsPipeline(Identification pipelineId,
            ref GraphicsPipelineDescription pipelineDescription)
        {
            var pipeline =
                VulkanEngine.GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(
                    ref pipelineDescription);
            _pipelines.Add(pipelineId, pipeline);
        }

        internal static void AddComputePipeline(Identification pipelineId,
            ref ComputePipelineDescription pipelineDescription)
        {
            var pipeline =
                VulkanEngine.GraphicsDevice.ResourceFactory
                    .CreateComputePipeline(ref pipelineDescription);
            _pipelines.Add(pipelineId, pipeline);
        }

        /// <summary>
        ///     Get a pipeline
        /// </summary>
        /// <param name="pipelineId"></param>
        /// <returns></returns>
        public static Pipeline GetPipeline(Identification pipelineId)
        {
            return _pipelines[pipelineId];
        }

        internal static void Clear()
        {
            foreach (var pipeline in _pipelines) pipeline.Value.Dispose();

            _pipelines.Clear();
        }
    }
}