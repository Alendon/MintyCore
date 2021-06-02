using System.Collections.Generic;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Render
{
    public static class PipelineHandler
    {
        private static Dictionary<Identification, Pipeline> _pipelines = new();

        internal static void AddGraphicsPipeline(Identification pipelineId, OutputDescription outputDescription,
            BlendStateDescription blendStateDescription, RasterizerStateDescription rasterizerStateDescription,
            ResourceLayout[] resourceLayouts, ShaderSetDescription shaderSetDescription,
            DepthStencilStateDescription depthStencilStateDescription, ResourceBindingModel? resourceBindingModel,
            PrimitiveTopology primitiveTopology)
        {
            GraphicsPipelineDescription pipelineDescription = new()
            {
                Outputs = outputDescription,
                BlendState = blendStateDescription,
                PrimitiveTopology = primitiveTopology,
                RasterizerState = rasterizerStateDescription,
                ResourceLayouts = resourceLayouts,
                ShaderSet = shaderSetDescription,
                DepthStencilState = depthStencilStateDescription,
                ResourceBindingModel = resourceBindingModel
            };

            AddGraphicsPipeline(pipelineId, ref pipelineDescription);
        }

        internal static void AddGraphicsPipeline(Identification pipelineId,
            ref GraphicsPipelineDescription pipelineDescription)
        {
            Pipeline pipeline =
                MintyCore.VulkanEngine.GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(
                    ref pipelineDescription);
            _pipelines.Add(pipelineId, pipeline);
        }

        internal static void AddComputePipeline(Identification pipelineId, Shader shader,
            SpecializationConstant[] specializations, ResourceLayout[] resourceLayouts,
            uint threadGroupSizeX,uint threadGroupSizeY,uint threadGroupSizeZ)
        {
            ComputePipelineDescription pipelineDescription = new()
            {
                ComputeShader = shader,
                Specializations = specializations,
                ResourceLayouts = resourceLayouts,
                ThreadGroupSizeX = threadGroupSizeX,
                ThreadGroupSizeY = threadGroupSizeY,
                ThreadGroupSizeZ = threadGroupSizeZ
            };
            AddComputePipeline(pipelineId, ref pipelineDescription);
        }

        internal static void AddComputePipeline(Identification pipelineId,
            ref ComputePipelineDescription pipelineDescription)
        {
            Pipeline pipeline =
                MintyCore.VulkanEngine.GraphicsDevice.ResourceFactory
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
    }
}