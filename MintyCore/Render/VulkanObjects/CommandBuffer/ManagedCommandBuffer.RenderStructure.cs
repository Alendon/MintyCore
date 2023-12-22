using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MintyCore.Render.Utils;
using Serilog;
using Silk.NET.Vulkan;

namespace MintyCore.Render.VulkanObjects;

public partial class ManagedCommandBuffer
{
    /// <summary>
    /// Begin recording the command buffer
    /// </summary>
    /// <remarks>
    /// This overload is for primary command buffers
    /// To begin a secondary command buffer, use <see cref="BeginSecondaryCommandBuffer"/>
    /// </remarks>
    public void BeginCommandBuffer(CommandBufferUsageFlags flags)
    {
        if (CommandBufferLevel == CommandBufferLevel.Secondary)
            throw new InvalidOperationException(
                $"Cannot call {nameof(BeginCommandBuffer)} on a secondary command buffer without inheritance info. Use the overload that accepts CommandBufferInheritanceInfo: `BeginCommandBuffer(CommandBufferUsageFlags, ref CommandBufferInheritanceInfo)`");

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = flags,
            PInheritanceInfo = null
        };

        VulkanUtils.Assert(VulkanEngine.Vk.BeginCommandBuffer(InternalCommandBuffer, in beginInfo));

        State = CommandBufferState.Recording;
    }

    /// <summary>
    /// Begins recording a secondary command buffer.
    /// </summary>
    /// <param name="flags">Usage flags for the command buffer.</param>
    /// <param name="renderPass">The render pass associated with the command buffer</param>
    /// <param name="frameBuffer">The frame buffer associated with the command buffer (optional).</param>
    /// <param name="subpass">The subpass index within the render pass (default is 0).</param>
    /// <param name="occlusionQuery">Specifies if occlusion queries are enabled (default is false).</param>
    /// <param name="queryFlags">Flags controlling the behavior of occlusion queries (default is 0).</param>
    /// <param name="pipelineStatistics">Flags controlling the statistics that are accumulated for the pipeline (default is 0).</param>
    /// <remarks>
    /// The render pass defines the compatible render passes when executing the secondary command buffer.
    /// It does not have to be the exact same render pass.
    /// The frame buffer must be compatible with the render pass. Passing the exact frame buffer may result in better performance.
    /// </remarks>
    public unsafe void BeginSecondaryCommandBuffer(CommandBufferUsageFlags flags,
        ManagedRenderPass renderPass, ManagedFrameBuffer? frameBuffer = null, uint subpass = 0,
        bool occlusionQuery = false, QueryControlFlags queryFlags = 0,
        QueryPipelineStatisticFlags pipelineStatistics = 0
    )
    {
        CommandBufferInheritanceInfo inheritanceInfo = new()
        {
            SType = StructureType.CommandBufferInheritanceInfo,
            Framebuffer = frameBuffer?.InternalFrameBuffer ?? default,
            RenderPass = renderPass.InternalRenderPass,
            Subpass = subpass,
            OcclusionQueryEnable = occlusionQuery,
            QueryFlags = queryFlags,
            PipelineStatistics = pipelineStatistics
        };

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = flags,
            PInheritanceInfo = (CommandBufferInheritanceInfo*) Unsafe.AsPointer(ref inheritanceInfo)
        };

        VulkanUtils.Assert(VulkanEngine.Vk.BeginCommandBuffer(InternalCommandBuffer, in beginInfo));

        State = CommandBufferState.Recording;
    }

    /// <summary>
    /// Ends recording of a command buffer, making it executable.
    /// </summary>
    public void EndCommandBuffer()
    {
        VulkanUtils.Assert(VulkanEngine.Vk.EndCommandBuffer(InternalCommandBuffer));

        State = CommandBufferState.Executable;
    }

    /// <summary>
    /// Gets the currently active render pass.
    /// </summary>
    /// <value>
    /// The currently active render pass, or <c>null</c> if there is no active render pass.
    /// </value>
    public ManagedRenderPass? CurrentRenderPass { get; private set; }

    /// <summary>
    /// Gets the index of the current subpass in a rendering pipeline.
    /// </summary>
    /// <value>
    /// The index of the current subpass.
    /// </value>
    public int CurrentSubpass { get; private set; }

    /// <summary>
    /// Gets the framebuffer associated with the currently active render pass.
    /// </summary>
    /// <value>The currently active framebuffer.</value>
    public ManagedFrameBuffer? CurrentFrameBuffer { get; private set; }

    /// <summary>
    /// Begins a render pass on a command buffer.
    /// </summary>
    /// <param name="renderPass">The render pass to begin.</param>
    /// <param name="frameBuffer">The frame buffer to use for rendering.</param>
    /// <param name="clearValues">The clear values for attachments in the render pass.</param>
    /// <param name="renderArea">The area within the frame buffer to render to.</param>
    /// <param name="subpassContents">The type of commands in the subpass that will be provided. The default is SubpassContents.Inline.</param>
    /// <exception cref="InvalidOperationException">Thrown when attempting to call BeginRenderPass on a non-recording command buffer, on a secondary command buffer, or while already in a render pass.</exception>
    public unsafe void BeginRenderPass(ManagedRenderPass renderPass, ManagedFrameBuffer frameBuffer,
        Span<ClearValue> clearValues, Rect2D renderArea, SubpassContents subpassContents = SubpassContents.Inline)
    {
        if (State != CommandBufferState.Recording)
            throw new InvalidOperationException($"Cannot call {nameof(BeginRenderPass)} on a non-recording command buffer");

        if (CommandBufferLevel == CommandBufferLevel.Secondary)
            throw new InvalidOperationException($"Cannot call {nameof(BeginRenderPass)} on a secondary command buffer");

        if (CurrentRenderPass is not null)
            throw new InvalidOperationException($"Cannot call {nameof(BeginRenderPass)} while already in a render pass");

        RenderPassBeginInfo beginInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = renderPass.InternalRenderPass,
            RenderArea = renderArea,
            Framebuffer = frameBuffer.InternalFrameBuffer,
            ClearValueCount = (uint) clearValues.Length,
            PClearValues = (ClearValue*) Unsafe.AsPointer(ref clearValues[0])
        };

        VulkanEngine.Vk.CmdBeginRenderPass(InternalCommandBuffer, in beginInfo, subpassContents);

        CurrentRenderPass = renderPass;
        CurrentFrameBuffer = frameBuffer;
        CurrentSubpass = 0;

        State = CommandBufferState.Recording;
    }

    /// <summary>
    /// Ends the current render pass on the command buffer.
    /// </summary>
    public void EndRenderPass()
    {
        if (State != CommandBufferState.Recording)
            throw new InvalidOperationException($"Cannot call {nameof(EndRenderPass)} on a non-recording command buffer");

        if (CommandBufferLevel == CommandBufferLevel.Secondary)
            throw new InvalidOperationException($"Cannot call {nameof(EndRenderPass)} on a secondary command buffer");

        if (CurrentRenderPass is null)
            throw new InvalidOperationException($"Cannot call {nameof(EndRenderPass)} while not in a render pass");

        VulkanEngine.Vk.CmdEndRenderPass(InternalCommandBuffer);

        CurrentRenderPass = null;
        CurrentFrameBuffer = null;
        CurrentSubpass = -1;
        
        BoundPipeline = null;

        State = CommandBufferState.Executable;
    }

    /// <summary>
    /// Advances to the next subpass within the current render pass.
    /// </summary>
    /// <param name="subpassContents">The contents of the next subpass.</param>
    /// <exception cref="InvalidOperationException">Thrown when:
    /// - The command buffer is not in recording state.
    /// - The command buffer is a secondary command buffer.
    /// - The method is called while not in a render pass.
    /// - There are no more subpasses in the current render pass.
    /// </exception>
    public void NextSubPass(SubpassContents subpassContents)
    {
        if(State != CommandBufferState.Recording)
            throw new InvalidOperationException($"Cannot call {nameof(NextSubPass)} on a non-recording command buffer");
        
        if (CommandBufferLevel == CommandBufferLevel.Secondary)
            throw new InvalidOperationException($"Cannot call {nameof(NextSubPass)} on a secondary command buffer");
        
        if (CurrentRenderPass is null)
            throw new InvalidOperationException($"Cannot call {nameof(NextSubPass)} while not in a render pass");
        
        if (CurrentSubpass + 1 >= CurrentRenderPass.SubpassCount)
            throw new InvalidOperationException($"Cannot call {nameof(NextSubPass)} when there are no more subpasses");
        
        VulkanEngine.Vk.CmdNextSubpass(InternalCommandBuffer, subpassContents);
        
        CurrentSubpass++;
        
        BoundPipeline = null;
    }

    /// <summary>
    /// Gets the currently bound pipeline.
    /// </summary>
    public ManagedPipeline? BoundPipeline { get; private set; }


    /// <summary>
    /// Binds a pipeline to the command buffer.
    /// </summary>
    /// <param name="pipeline">The pipeline to be bound.</param>
    /// <exception cref="InvalidOperationException">Thrown when the command buffer is not in the recording state or not inside a render pass.</exception>
    public void BindPipeline(ManagedPipeline pipeline)
    {
        if (State != CommandBufferState.Recording)
            throw new InvalidOperationException($"Cannot call {nameof(BindPipeline)} on a non-recording command buffer");
        
        if(CurrentRenderPass is null)
            throw new InvalidOperationException($"Cannot call {nameof(BindPipeline)} while not in a render pass");

        if (BoundPipeline == pipeline)
            return;

        VulkanEngine.Vk.CmdBindPipeline(InternalCommandBuffer, PipelineBindPoint.Graphics, pipeline.InternalPipeline);

        BoundPipeline = pipeline;
    }
}