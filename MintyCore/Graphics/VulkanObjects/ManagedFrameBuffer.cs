using JetBrains.Annotations;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.VulkanObjects;

/// <summary>
/// A managed version of the vulkan <see cref="Framebuffer"/>
/// </summary>
/// <remarks>This is not fully implemented yet</remarks>
[PublicAPI]
public class ManagedFrameBuffer : VulkanObject
{
    /// <summary>
    /// Gets the internal frame buffer.
    /// </summary>

    public Framebuffer InternalFrameBuffer { get; }

    /// <summary>
    /// Initializes a new instance of the ManagedFrameBuffer class.
    /// </summary>
    /// <param name="vulkanEngine">The Vulkan engine.</param>
    /// <param name="internalFrameBuffer">The internal frame buffer.</param>
    public ManagedFrameBuffer(IVulkanEngine vulkanEngine, Framebuffer internalFrameBuffer) : base(vulkanEngine)
    {
        InternalFrameBuffer = internalFrameBuffer;
    }

    /// <summary>
    /// Initializes a new instance of the ManagedFrameBuffer class with a specified allocation handler.
    /// </summary>
    /// <param name="vulkanEngine">The Vulkan engine.</param>
    /// <param name="allocationHandler">The allocation handler.</param>
    /// <param name="internalFrameBuffer">The internal frame buffer.</param>
    public ManagedFrameBuffer(IVulkanEngine vulkanEngine, IAllocationHandler allocationHandler,
        Framebuffer internalFrameBuffer) : base(vulkanEngine, allocationHandler)
    {
        InternalFrameBuffer = internalFrameBuffer;
    }
}