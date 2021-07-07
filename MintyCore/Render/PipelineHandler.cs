using System;
using System.Collections.Generic;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Render
{
    public static class PipelineHandler
    {
        private static Dictionary<Identification, Pipeline> _pipelines = new();

        internal static void AddGraphicsPipeline(Identification pipelineId,
            ref GraphicsPipelineDescription pipelineDescription)
        { 
            Pipeline pipeline =
                VulkanEngine.GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(
                    ref pipelineDescription);
            _pipelines.Add(pipelineId, pipeline);
        }

        internal static void AddComputePipeline(Identification pipelineId,
            ref ComputePipelineDescription pipelineDescription)
        {
            Pipeline pipeline =
                VulkanEngine.GraphicsDevice.ResourceFactory
                    .CreateComputePipeline(ref pipelineDescription);
            _pipelines.Add(pipelineId, pipeline);
        }

        public static Pipeline GetPipeline(Identification pipelineId)
        {
            return _pipelines[pipelineId];
        }

        internal static void Clear()
        {
            foreach (var pipeline in _pipelines)
            {
                pipeline.Value.Dispose();
            }

            _pipelines.Clear();
        }

		internal static Pipeline GetPipeline(object wireFrame)
		{
			throw new NotImplementedException();
		}
	}
}