using JetBrains.Annotations;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.VulkanObjects;

/// <summary>
///  A managed version of the vulkan <see cref="Pipeline"/>
/// <remarks>This is not fully implemented yet</remarks>
/// </summary>
[PublicAPI]
public class ManagedPipeline : VulkanObject
{
    /// <summary>
    /// Gets the internal Vulkan Pipeline.
    /// </summary>
    public Pipeline InternalPipeline { get; }

    /// <summary>
    /// Gets the internal Vulkan Pipeline Layout.
    /// </summary>
    public PipelineLayout InternalPipelineLayout { get; }

    /// <summary>
    /// Initializes a new instance of the ManagedPipeline class.
    /// </summary>
    /// <param name="vulkanEngine">The Vulkan engine.</param>
    public ManagedPipeline(IVulkanEngine vulkanEngine) : base(vulkanEngine)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ManagedPipeline class with a specified allocation handler.
    /// </summary>
    /// <param name="vulkanEngine">The Vulkan engine.</param>
    /// <param name="allocationHandler">The allocation handler.</param>
    public ManagedPipeline(IVulkanEngine vulkanEngine, IAllocationHandler? allocationHandler) : base(vulkanEngine,
        allocationHandler)
    {
    }
}