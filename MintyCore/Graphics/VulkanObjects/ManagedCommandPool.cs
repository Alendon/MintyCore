using JetBrains.Annotations;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Utils;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.VulkanObjects;

/// <summary>
/// Represents a managed command pool in Vulkan.
/// </summary>
[PublicAPI]
public class ManagedCommandPool : VulkanObject
{
    /// <inheritdoc />
    public ManagedCommandPool(IVulkanEngine vulkanEngine, CommandPool internalCommandPool,
        CommandPoolDescription description) : base(vulkanEngine)
    {
        InternalCommandPool = internalCommandPool;
        IsResettable = description.IsResettable;
        IsTransient = description.IsTransient;
        IsProtected = description.IsProtected;
        QueueFamilyIndex = description.QueueFamilyIndex;
    }

    /// <inheritdoc />
    public ManagedCommandPool(IVulkanEngine vulkanEngine, IAllocationHandler? allocationHandler,
        CommandPool internalCommandPool, CommandPoolDescription description) : base(vulkanEngine, allocationHandler)
    {
        InternalCommandPool = internalCommandPool;
        IsResettable = description.IsResettable;
        IsTransient = description.IsTransient;
        IsProtected = description.IsProtected;
        QueueFamilyIndex = description.QueueFamilyIndex;
    }

    /// <summary>
    /// Gets the internal command pool instance.
    /// </summary>
    /// <value>
    /// The internal command pool instance.
    /// </value>
    public CommandPool InternalCommandPool { get; }

    /// <summary>
    /// Whether individual command buffers can be reset or not
    /// </summary>
    public bool IsResettable { get; }

    /// <summary>
    ///  Whether the command pool is transient or not
    /// </summary>
    public bool IsTransient { get; }

    /// <summary>
    ///  Whether the command pool is protected or not
    /// </summary>
    public bool IsProtected { get; }

    /// <summary>
    /// Gets the index of the associated queue family.
    /// </summary>
    public uint QueueFamilyIndex { get; }

    /// <summary>
    ///   Resets the command pool
    /// </summary>
    /// <param name="releaseResources"> Whether to release resources or not </param>
    public void Reset(bool releaseResources = false)
    {
        VulkanUtils.Assert(VulkanEngine.Vk.ResetCommandPool(VulkanEngine.Device, InternalCommandPool,
            releaseResources ? CommandPoolResetFlags.ReleaseResourcesBit : 0));
    }


    /// <summary>
    /// Allocates a command buffer from the command pool with the given level.
    /// </summary>
    /// <param name="level">The level of the command buffer to allocate.</param>
    /// <returns>A new instance of the ManagedCommandBuffer class.</returns>
    public ManagedCommandBuffer AllocateCommandBuffer(CommandBufferLevel level)
    {
        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = InternalCommandPool,
            Level = level,
            CommandBufferCount = 1
        };

        VulkanUtils.Assert(
            VulkanEngine.Vk.AllocateCommandBuffers(VulkanEngine.Device, allocateInfo, out var commandBuffer));

        return new ManagedCommandBuffer(VulkanEngine, AllocationHandler, commandBuffer, this, level);
    }


    /// <summary>
    /// Frees a command buffer.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to free.</param>
    public void FreeCommandBuffer(ManagedCommandBuffer commandBuffer)
    {
        VulkanEngine.Vk.FreeCommandBuffers(VulkanEngine.Device, InternalCommandPool, 1,
            commandBuffer.InternalCommandBuffer);
    }

    /// <summary>
    /// Trims the internal command pool of the VulkanEngine.
    /// </summary>
    public void Trim()
    {
        VulkanEngine.Vk.TrimCommandPool(VulkanEngine.Device, InternalCommandPool, 0);
    }

    /// <inheritdoc />
    protected override unsafe void ReleaseUnmanagedResources()
    {
        base.ReleaseUnmanagedResources();
        VulkanEngine.Vk.DestroyCommandPool(VulkanEngine.Device, InternalCommandPool, null);
    }
}