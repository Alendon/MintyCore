using System;
using JetBrains.Annotations;
using MintyCore.Graphics.Utils;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.VulkanObjects;

/// <summary>
/// Managed version of a command buffer.
/// </summary>
/// <remarks>Currently the ManagedCommandBuffer is only partially implemented</remarks>
[PublicAPI]
public partial class ManagedCommandBuffer : VulkanObject
{
    /// <inheritdoc />
    public ManagedCommandBuffer(IVulkanEngine vulkanEngine, CommandBuffer internalCommandBuffer, ManagedCommandPool? parentPool, CommandBufferLevel commandBufferLevel) : base(vulkanEngine)
    {
        InternalCommandBuffer = internalCommandBuffer;
        ParentPool = parentPool;
        CommandBufferLevel = commandBufferLevel;
    }

    /// <inheritdoc />
    public ManagedCommandBuffer(IVulkanEngine vulkanEngine, IAllocationHandler? allocationHandler, CommandBuffer internalCommandBuffer, ManagedCommandPool? parentPool, CommandBufferLevel commandBufferLevel) : base(vulkanEngine, allocationHandler)
    {
        InternalCommandBuffer = internalCommandBuffer;
        ParentPool = parentPool;
        CommandBufferLevel = commandBufferLevel;
    }

    /// <summary>
    /// Gets the internal command buffer.
    /// </summary>
    /// <remarks>
    /// It is not recommended to use the native command buffer
    /// Use a managed overload if possible
    /// </remarks>
    /// <value>
    /// The internal command buffer.
    /// </value>
    public CommandBuffer InternalCommandBuffer { get; private set; }

    /// <summary>
    /// Gets the level of a command buffer.
    /// </summary>
    /// <returns>The level of the command buffer.</returns>
    public CommandBufferLevel CommandBufferLevel { get; }

    private ManagedCommandPool? ParentPool { get; set; }

    /// <summary>
    /// Represents the state of a command buffer.
    /// </summary>
    /// <value>
    /// The current state of the command buffer.
    /// </value>
    public CommandBufferState State { get; private set; } = CommandBufferState.Initial;

    /// <summary>
    /// Resets the command buffer to its initial state.
    /// This is only possible if the command buffer is allocated from a resettable pool.
    /// </summary>
    public void Reset()
    {
        if (ParentPool?.IsResettable is false)
            throw new InvalidOperationException("Cannot reset a command buffer from a non-resettable pool");
        
        VulkanUtils.Assert(VulkanEngine.Vk.ResetCommandBuffer(InternalCommandBuffer, 0));
        
        State = CommandBufferState.Initial;
    }

    /// <summary>
    /// Executes a secondary command buffer.
    /// </summary>
    /// <param name="commandBuffer">The secondary command buffer to execute.</param>
    /// <exception cref="InvalidOperationException">Thrown when trying to execute from a secondary command buffer, or when trying to execute a primary command buffer.</exception>
    public void ExecuteSecondary(ManagedCommandBuffer commandBuffer)
    {
        if (CommandBufferLevel != CommandBufferLevel.Primary)
            throw new InvalidOperationException("Cannot execute a secondary command buffer from a secondary command buffer");
        
        if(commandBuffer.CommandBufferLevel != CommandBufferLevel.Secondary)
            throw new InvalidOperationException("Cannot execute a primary command buffer from a primary command buffer");
        
        VulkanEngine.Vk.CmdExecuteCommands(InternalCommandBuffer, 1, commandBuffer.InternalCommandBuffer);
    }
    
    /// <inheritdoc />
    protected override void ReleaseManagedResources()
    {
        base.ReleaseManagedResources();

        ParentPool?.FreeCommandBuffer(this);

        ParentPool = null;
        InternalCommandBuffer = default;
    }

    
}