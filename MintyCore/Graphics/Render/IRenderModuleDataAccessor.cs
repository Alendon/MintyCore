using System;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data;
using MintyCore.Utils;
using OneOf;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render;

[PublicAPI]
public interface IRenderModuleDataAccessor
{
    Func<TIntermediateData?> UseIntermediateData<TIntermediateData>(Identification intermediateDataId,
        RenderModule renderModule) where TIntermediateData : IntermediateData;
    
    void SetColorAttachment(OneOf<Identification, Swapchain> targetTexture, RenderModule renderModule);
    void SetDepthStencilAttachment(Identification targetDepthTexture, RenderModule renderModule);
    
    Func<DescriptorSet> UseSampledTexture(Identification textureId, RenderModule renderModule);
    Func<DescriptorSet> UseStorageTexture(Identification textureId, RenderModule renderModule);
}