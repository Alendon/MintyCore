using System;
using MintyCore.Render;
using Silk.NET.Vulkan;
using static MintyCore.Render.VulkanUtils;

namespace Vulkanizer
{
    public unsafe class GraphicsPipelineBuilder
    {
        private GraphicsPipelineCreateInfo _createInfo;
       
        private PipelineShaderStageCreateInfo[] _shaderStageCreateInfo = Array.Empty<PipelineShaderStageCreateInfo>();
        private PipelineDynamicStateCreateInfo _dynamicStateCreateInfo;
        private PipelineMultisampleStateCreateInfo _multisampleStateCreateInfo;
        private PipelineRasterizationStateCreateInfo _rasterizationStateCreateInfo;
        private PipelineTessellationStateCreateInfo _tessellationStateCreateInfo;
        private PipelineVertexInputStateCreateInfo _vertexInputStateCreateInfo;
        private PipelineInputAssemblyStateCreateInfo _inputAssemblyStateCreateInfo;
        private PipelineDepthStencilStateCreateInfo _depthStencilStateCreateInfo;
        private PipelineColorBlendStateCreateInfo _colorBlendStateCreateInfo;
        private PipelineViewportStateCreateInfo _viewportStateCreateInfo;


        internal GraphicsPipelineBuilder()
        {
            _createInfo = new()
            {
                PNext = null,
                SType = StructureType.GraphicsPipelineCreateInfo,
            };
        }

        public Pipeline Build()
        {
            //Assign the missing pointers in the create info by using local variables
            
            PipelineShaderStageCreateInfo* shaderStageCreateInfo =
                stackalloc PipelineShaderStageCreateInfo[_shaderStageCreateInfo.Length];
            if (_createInfo.PStages is null && _shaderStageCreateInfo.Length != 0)
            {
                for (int index = 0; index < _shaderStageCreateInfo.Length; index++)
                {
                    shaderStageCreateInfo[index] = _shaderStageCreateInfo[index];
                }

                _createInfo.PStages = shaderStageCreateInfo;
                _createInfo.StageCount = (uint)_shaderStageCreateInfo.Length;
            }

            PipelineDynamicStateCreateInfo lDynamicStateCreateInfo = default;
            PipelineMultisampleStateCreateInfo lMultisampleStateCreateInfo = default;
            PipelineRasterizationStateCreateInfo lRasterizationStateCreateInfo = default;
            PipelineTessellationStateCreateInfo lTessellationStateCreateInfo = default;
            PipelineVertexInputStateCreateInfo lVertexInputStateCreateInfo = default;
            PipelineInputAssemblyStateCreateInfo lInputAssemblyStateCreateInfo = default;
            PipelineDepthStencilStateCreateInfo lDepthStencilStateCreateInfo = default;
            PipelineColorBlendStateCreateInfo lColorBlendStateCreateInfo = default;
            PipelineViewportStateCreateInfo lViewportStateCreateInfo = default;

            if (_createInfo.PDynamicState is null &&
                _dynamicStateCreateInfo.SType == StructureType.PipelineDynamicStateCreateInfo)
            {
                lDynamicStateCreateInfo = _dynamicStateCreateInfo;
                _createInfo.PDynamicState = &lDynamicStateCreateInfo;
            }
            if (_createInfo.PMultisampleState is null &&
                _multisampleStateCreateInfo.SType == StructureType.PipelineMultisampleStateCreateInfo)
            {
                lMultisampleStateCreateInfo = _multisampleStateCreateInfo;
                _createInfo.PMultisampleState = &lMultisampleStateCreateInfo;
            }
            if (_createInfo.PRasterizationState is null &&
                _rasterizationStateCreateInfo.SType == StructureType.PipelineRasterizationStateCreateInfo)
            {
                lRasterizationStateCreateInfo = _rasterizationStateCreateInfo;
                _createInfo.PRasterizationState = &lRasterizationStateCreateInfo;
            }
            if (_createInfo.PTessellationState is null &&
                _tessellationStateCreateInfo.SType == StructureType.PipelineTessellationStateCreateInfo)
            {
                lTessellationStateCreateInfo = _tessellationStateCreateInfo;
                _createInfo.PTessellationState = &lTessellationStateCreateInfo;
            }
            if (_createInfo.PVertexInputState is null &&
                _vertexInputStateCreateInfo.SType == StructureType.PipelineVertexInputStateCreateInfo)
            {
                lVertexInputStateCreateInfo = _vertexInputStateCreateInfo;
                _createInfo.PVertexInputState = &lVertexInputStateCreateInfo;
            }
            if (_createInfo.PInputAssemblyState is null &&
                _inputAssemblyStateCreateInfo.SType == StructureType.PipelineInputAssemblyStateCreateInfo)
            {
                lInputAssemblyStateCreateInfo = _inputAssemblyStateCreateInfo;
                _createInfo.PInputAssemblyState = &lInputAssemblyStateCreateInfo;
            }
            if (_createInfo.PDepthStencilState is null &&
                _depthStencilStateCreateInfo.SType == StructureType.PipelineDepthStencilStateCreateInfo)
            {
                lDepthStencilStateCreateInfo = _depthStencilStateCreateInfo;
                _createInfo.PDepthStencilState = &lDepthStencilStateCreateInfo;
            }
            if (_createInfo.PColorBlendState is null &&
                _colorBlendStateCreateInfo.SType == StructureType.PipelineColorBlendStateCreateInfo)
            {
                lColorBlendStateCreateInfo = _colorBlendStateCreateInfo;
                _createInfo.PColorBlendState = &lColorBlendStateCreateInfo;
            }
            if (_createInfo.PViewportState is null &&
                _viewportStateCreateInfo.SType == StructureType.PipelineViewportStateCreateInfo)
            {
                lViewportStateCreateInfo = _viewportStateCreateInfo;
                _createInfo.PViewportState = &lViewportStateCreateInfo;
            }
            
            //Create the Graphics Pipeline
            Assert(VulkanEngine._vk.CreateGraphicsPipelines(VulkanEngine._device, default, 1, _createInfo,
                VulkanEngine._allocationCallback, out Pipeline pipeline));
            
            //Clear the local pointers from the create info
            if (lDynamicStateCreateInfo.SType == StructureType.PipelineDynamicStateCreateInfo)
            {
                _createInfo.PDynamicState = null;
            }
            if (lMultisampleStateCreateInfo.SType == StructureType.PipelineMultisampleStateCreateInfo)
            {
                _createInfo.PMultisampleState = null;
            }
            if (lRasterizationStateCreateInfo.SType == StructureType.PipelineRasterizationStateCreateInfo)
            {
                _createInfo.PRasterizationState = null;
            }
            if (lTessellationStateCreateInfo.SType == StructureType.PipelineTessellationStateCreateInfo)
            {
                _createInfo.PTessellationState = null;
            }
            if (lVertexInputStateCreateInfo.SType == StructureType.PipelineVertexInputStateCreateInfo)
            {
                _createInfo.PVertexInputState = null;
            }
            if (lInputAssemblyStateCreateInfo.SType == StructureType.PipelineInputAssemblyStateCreateInfo)
            {
                _createInfo.PInputAssemblyState = null;
            }
            if (lDepthStencilStateCreateInfo.SType == StructureType.PipelineDepthStencilStateCreateInfo)
            {
                _createInfo.PDepthStencilState = null;
            }
            if (lColorBlendStateCreateInfo.SType == StructureType.PipelineColorBlendStateCreateInfo)
            {
                _createInfo.PColorBlendState = null;
            }
            if (lViewportStateCreateInfo.SType == StructureType.PipelineViewportStateCreateInfo)
            {
                _createInfo.PViewportState = null;
            }
            
            return pipeline;
        }

        public void SetViewportState(PipelineViewportStateCreateInfo createInfo)
        {
            _viewportStateCreateInfo = createInfo;
        }

        public void SetViewportState(PipelineViewportStateCreateInfo* createInfo)
        {
            _createInfo.PViewportState = createInfo;
        }

        public void SetColorBlendState(PipelineColorBlendStateCreateInfo createInfo)
        {
            _colorBlendStateCreateInfo = createInfo;
        }

        public void SetColorBlendState(PipelineColorBlendStateCreateInfo* createInfo)
        {
            _createInfo.PColorBlendState = createInfo;
        }

        public void SetDepthStencilState(PipelineDepthStencilStateCreateInfo createInfo)
        {
            _depthStencilStateCreateInfo = createInfo;
        }

        public void SetDepthStencilState(PipelineDepthStencilStateCreateInfo* createInfo)
        {
            _createInfo.PDepthStencilState = createInfo;
        }

        public void SetInputAssemblyState(PipelineInputAssemblyStateCreateInfo createInfo)
        {
            _inputAssemblyStateCreateInfo = createInfo;
        }

        public void SetInputAssemblyState(PipelineInputAssemblyStateCreateInfo* createInfo)
        {
            _createInfo.PInputAssemblyState = createInfo;
        }

        public void SetVertexInputState(PipelineVertexInputStateCreateInfo createInfo)
        {
            _vertexInputStateCreateInfo = createInfo;
        }

        public void SetVertexInputState(PipelineVertexInputStateCreateInfo* createInfo)
        {
            _createInfo.PVertexInputState = createInfo;
        }

        public void SetTessellationState(PipelineTessellationStateCreateInfo createInfo)
        {
            _tessellationStateCreateInfo = createInfo;
        }

        public void SetTessellationState(PipelineTessellationStateCreateInfo* createInfo)
        {
            _createInfo.PTessellationState = createInfo;
        }

        public void SetRasterizationState(PipelineRasterizationStateCreateInfo createInfo)
        {
            _rasterizationStateCreateInfo = createInfo;
        }

        public void SetRasterizationState(PipelineRasterizationStateCreateInfo* createInfo)
        {
            _createInfo.PRasterizationState = createInfo;
        }

        public void SetMultiSampleState(PipelineMultisampleStateCreateInfo createInfo)
        {
            _multisampleStateCreateInfo = createInfo;
        }

        public void SetMultiSampleState(PipelineMultisampleStateCreateInfo* createInfo)
        {
            _createInfo.PMultisampleState = createInfo;
        }

        public void SetDynamicState(PipelineDynamicStateCreateInfo createInfo)
        {
            _dynamicStateCreateInfo = createInfo;
        }

        public void SetDynamicState(PipelineDynamicStateCreateInfo* createInfo)
        {
            _createInfo.PDynamicState = createInfo;
        }

        public void SetBasePipelineIndex(int index)
        {
            _createInfo.BasePipelineIndex = index;
        }

        public void SetBasePipelineHandle(Pipeline pipeline)
        {
            _createInfo.BasePipelineHandle = pipeline;
        }

        public void SetRenderPass(RenderPass renderPass)
        {
            _createInfo.RenderPass = renderPass;
        }

        public void SetShaderStages(PipelineShaderStageCreateInfo[] stages)
        {
            _shaderStageCreateInfo = stages;
        }

        public void SetShaderStages(PipelineShaderStageCreateInfo* stages, uint stageCount)
        {
            _createInfo.PStages = stages;
            _createInfo.StageCount = stageCount;
        }

        public void SetFlags(PipelineCreateFlags flags)
        {
            _createInfo.Flags = flags;
        }

        public void SetLayout(PipelineLayout layout)
        {
            _createInfo.Layout = layout;
        }

        public void SetSubpass(uint subpass)
        {
            _createInfo.Subpass = subpass;
        }
    }
}