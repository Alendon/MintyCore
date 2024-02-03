using System;
using MintyCore.Render.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.VulkanObjects;

public partial class ManagedCommandBuffer
{
    public void ClearColorImage(Texture texture, ClearColorValue clearValue, ImageSubresourceRange subresourceRange,
        ImageLayout layout)
    {
        if (State != CommandBufferState.Recording)
            throw new InvalidOperationException("Command buffer must be in recording state to clear an image");

        VulkanEngine.Vk.CmdClearColorImage(InternalCommandBuffer, texture.Image, layout, clearValue, 1,
            subresourceRange);
    }

    public void ClearDepthStencilImage(Texture texture, ClearDepthStencilValue clearValue,
        ImageSubresourceRange subresourceRange, ImageLayout layout)
    {
        if (State != CommandBufferState.Recording)
            throw new InvalidOperationException("Command buffer must be in recording state to clear an image");

        VulkanEngine.Vk.CmdClearDepthStencilImage(InternalCommandBuffer, texture.Image, layout, clearValue, 1,
            subresourceRange);
    }

    public void PipelineBarrier(PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        DependencyFlags dependencyFlags, ReadOnlySpan<MemoryBarrier> memoryBarriers)
    {
         PipelineBarrier(srcStage, dstStage, dependencyFlags, memoryBarriers, ReadOnlySpan<BufferMemoryBarrier>.Empty,
            ReadOnlySpan<ImageMemoryBarrier>.Empty);
    }

    public void PipelineBarrier(PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        DependencyFlags dependencyFlags, ReadOnlySpan<BufferMemoryBarrier> bufferBarriers)
    {
        PipelineBarrier(srcStage, dstStage, dependencyFlags, ReadOnlySpan<MemoryBarrier>.Empty, bufferBarriers,
            ReadOnlySpan<ImageMemoryBarrier>.Empty);
    }

    public void PipelineBarrier(PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        DependencyFlags dependencyFlags, ReadOnlySpan<ImageMemoryBarrier> imageBarriers)
    {
        PipelineBarrier(srcStage, dstStage, dependencyFlags, ReadOnlySpan<MemoryBarrier>.Empty,
            ReadOnlySpan<BufferMemoryBarrier>.Empty, imageBarriers);
    }
    
    public void PipelineBarrier(PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        DependencyFlags dependencyFlags,
        ReadOnlySpan<MemoryBarrier> memoryBarriers, ReadOnlySpan<BufferMemoryBarrier> bufferBarriers,
        ReadOnlySpan<ImageMemoryBarrier> imageBarriers)
    {
        if (State != CommandBufferState.Recording)
            throw new InvalidOperationException(
                "Command buffer must be in recording state to execute a pipeline barrier");

        VulkanEngine.Vk.CmdPipelineBarrier(InternalCommandBuffer, srcStage, dstStage, dependencyFlags,
            memoryBarriers, bufferBarriers, imageBarriers);
    }

    public void CopyBuffer(MemoryBuffer src, MemoryBuffer dst)
    {
        BufferCopy region = new()
        {
            Size = Math.Min(src.Size, dst.Size),
            DstOffset = 0,
            SrcOffset = 0
        };
        CopyBuffer(src, dst, region);
    }
    
    public unsafe void CopyBuffer(MemoryBuffer src, MemoryBuffer dst, BufferCopy region)
    {
        CopyBuffer(src, dst, new ReadOnlySpan<BufferCopy>(&region, 1));
    }
    
    public void CopyBuffer(MemoryBuffer src, MemoryBuffer dst, ReadOnlySpan<BufferCopy> regions)
    {
        if (State != CommandBufferState.Recording)
            throw new InvalidOperationException("Command buffer must be in recording state to copy a buffer");

        VulkanEngine.Vk.CmdCopyBuffer(InternalCommandBuffer, src.Buffer, dst.Buffer, regions);
    }
}