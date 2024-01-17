using JetBrains.Annotations;
using MintyCore.Utils;
using OneOf;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.RenderGraph;

/// <summary>
/// Represents the access details of a render resource.
/// </summary>
/// <param name="RenderResourceId">The identification of the render resource.</param>
/// <param name="PipelineStage">The stage at which the render resource is accessed.</param>
/// <param name="AccessMode">The mode of access for the render resource.</param>
[PublicAPI]
public record RenderResourceAccess(
    Identification RenderResourceId,
    OneOf<PipelineStageFlags, (PipelineStageFlags start, PipelineStageFlags end)> PipelineStage,
    OneOf<AccessFlags, (AccessFlags start, AccessFlags end)> AccessMode,
    OneOf<RenderResourceAccess.Populate, RenderResourceAccess.Intermediate, RenderResourceAccess.Consume> AccessType,
    ImageLayout? ImageLayout = null
)
{
    /// <summary>
    /// Defines that the render module working with this resource is populating it.
    /// Only one render module can populate a resource.
    /// This render module will automatically be executed before any other render modules working with this resource.
    /// </summary>
    public struct Populate;

    /// <summary>
    /// Defines that the render module working with this resource is a intermediate activity.
    /// This means that changes to the resource can happen before and after this render module.
    /// It will be executed after the population and before consumption but internal ordering needs to be defined explicitly.
    /// </summary>
    public struct Intermediate;
    
    /// <summary>
    /// Defines that the render module working with this resource only consumes it. (read only)
    /// This render module will be executed after all other modules writing to this resource.
    /// </summary>
    public struct Consume;
}