using System;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data;
using MintyCore.Utils;
using OneOf;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render;

/// <summary>
/// Interface to access render module data.
/// </summary>
[PublicAPI]
public interface IRenderModuleDataAccessor
{
    /// <summary>
    /// Uses intermediate data.
    /// </summary>
    /// <typeparam name="TIntermediateData">The type of the intermediate data.</typeparam>
    /// <param name="intermediateDataId">The identification of the intermediate data.</param>
    /// <param name="renderModule">The render module using the data.</param>
    /// <returns>A function that returns the intermediate data.</returns>
    Func<TIntermediateData?> UseIntermediateData<TIntermediateData>(Identification intermediateDataId,
        RenderModule renderModule) where TIntermediateData : IntermediateData;

    /// <summary>
    /// Sets the color attachment of the render module.
    /// </summary>
    /// <param name="targetTexture"> The target texture to set as color attachment. </param>
    /// <param name="renderModule"> The render module to set the color attachment for. </param>
    void SetColorAttachment(OneOf<Identification, Swapchain> targetTexture, RenderModule renderModule);

    /// <summary>
    ///  Sets the depth stencil attachment of the render module.
    /// </summary>
    /// <param name="targetDepthTexture"> The target depth texture to set as depth stencil attachment. </param>
    /// <param name="renderModule"> The render module to set the depth stencil attachment for. </param>
    void SetDepthStencilAttachment(Identification targetDepthTexture, RenderModule renderModule);

    /// <summary>
    ///  Use a sampled texture.
    /// </summary>
    /// <param name="textureId"> The identification of the texture. </param>
    /// <param name="renderModule"> The render module using the texture. </param>
    /// <returns > A function that returns the descriptor set for binding the texture. </returns>
    Func<DescriptorSet> UseSampledTexture(Identification textureId, RenderModule renderModule, ColorAttachmentSampleMode sampleMode = ColorAttachmentSampleMode.Linear);

    /// <summary>
    ///  Use a storage texture.
    /// </summary>
    /// <param name="textureId"> The identification of the texture. </param>
    /// <param name="renderModule"> The render module using the texture. </param>
    /// <returns> A function that returns the descriptor set for binding the texture. </returns>
    Func<DescriptorSet> UseStorageTexture(Identification textureId, RenderModule renderModule);
}

public enum ColorAttachmentSampleMode
{
    Linear,
    Nearest
}