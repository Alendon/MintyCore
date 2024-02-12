using JetBrains.Annotations;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.VulkanObjects;

/// <summary>
///  A managed version of the vulkan <see cref="RenderPass"/>
/// <remarks>This is not fully implemented yet</remarks>
/// </summary>
[PublicAPI]
public class ManagedRenderPass : VulkanObject
{
    /// <summary>
    /// Gets the internal Vulkan Render Pass.
    /// </summary>
    public RenderPass InternalRenderPass { get; }

    /// <summary>
    /// Gets the count of subpasses in the Render Pass.
    /// </summary>
    public uint SubpassCount { get; }

    /// <summary>
    /// Initializes a new instance of the ManagedRenderPass class.
    /// </summary>
    /// <param name="vulkanEngine">The Vulkan engine.</param>
    public ManagedRenderPass(IVulkanEngine vulkanEngine) : base(vulkanEngine)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ManagedRenderPass class with a specified allocation handler.
    /// </summary>
    /// <param name="vulkanEngine">The Vulkan engine.</param>
    /// <param name="allocationHandler">The allocation handler.</param>
    public ManagedRenderPass(IVulkanEngine vulkanEngine, IAllocationHandler? allocationHandler) : base(vulkanEngine,
        allocationHandler)
    {
    }
}