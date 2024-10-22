﻿using System;
using MintyCore.Graphics.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.VulkanObjects;

public partial class ManagedCommandBuffer
{
    /// <summary>
    /// Clears the color image.
    /// </summary>
    /// <param name="texture"> The texture to clear. </param>
    /// <param name="clearValue"> The clear value. </param>
    /// <param name="subresourceRange"> The range of the image to clear. </param>
    /// <param name="layout"> The layout of the image. </param>
    /// <exception cref="InvalidOperationException"></exception>
    public void ClearColorImage(Texture texture, ClearColorValue clearValue, ImageSubresourceRange subresourceRange,
        ImageLayout layout)
    {
        if (State != CommandBufferState.Recording)
            throw new InvalidOperationException("Command buffer must be in recording state to clear an image");

        VulkanEngine.Vk.CmdClearColorImage(InternalCommandBuffer, texture.Image, layout, clearValue, 1,
            subresourceRange);
    }

    /// <summary>
    ///  Clears the depth stencil image.
    /// </summary>
    /// <param name="texture">The texture to clear.</param>
    /// <param name="clearValue">The clear value.</param>
    /// <param name="subresourceRange">The range of the image to clear.</param>
    /// <param name="layout">The layout of the image.</param>
    /// <exception cref="InvalidOperationException">Thrown when the command buffer is not in recording state.</exception>
    public void ClearDepthStencilImage(Texture texture, ClearDepthStencilValue clearValue,
        ImageSubresourceRange subresourceRange, ImageLayout layout)
    {
        if (State != CommandBufferState.Recording)
            throw new InvalidOperationException("Command buffer must be in recording state to clear an image");

        VulkanEngine.Vk.CmdClearDepthStencilImage(InternalCommandBuffer, texture.Image, layout, clearValue, 1,
            subresourceRange);
    }

    /// <summary>
    /// Inserts a memory barrier into the command buffer.
    /// </summary>
    /// <param name="srcStage">The source stage of the pipeline.</param>
    /// <param name="dstStage">The destination stage of the pipeline.</param>
    /// <param name="dependencyFlags">Dependency flags.</param>
    /// <param name="memoryBarriers">Memory barriers to insert.</param>
    public void PipelineBarrier(PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        DependencyFlags dependencyFlags, ReadOnlySpan<MemoryBarrier> memoryBarriers)
    {
        PipelineBarrier(srcStage, dstStage, dependencyFlags, memoryBarriers, ReadOnlySpan<BufferMemoryBarrier>.Empty,
            ReadOnlySpan<ImageMemoryBarrier>.Empty);
    }

    /// <summary>
    /// Inserts a memory barrier into the command buffer.
    /// </summary>
    /// <param name="srcStage">The source stage of the pipeline.</param>
    /// <param name="dstStage">The destination stage of the pipeline.</param>
    /// <param name="dependencyFlags">Dependency flags.</param>
    /// <param name="memoryBarrier">Memory barrier to insert.</param>
    public void PipelineBarrier(PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        DependencyFlags dependencyFlags, MemoryBarrier memoryBarrier)
    {
        Span<MemoryBarrier> memoryBarriers = [memoryBarrier];

        PipelineBarrier(srcStage, dstStage, dependencyFlags, memoryBarriers, ReadOnlySpan<BufferMemoryBarrier>.Empty,
            ReadOnlySpan<ImageMemoryBarrier>.Empty);
    }

    /// <summary>
    /// Inserts a buffer memory barrier into the command buffer.
    /// </summary>
    /// <param name="srcStage">The source stage of the pipeline.</param>
    /// <param name="dstStage">The destination stage of the pipeline.</param>
    /// <param name="dependencyFlags">Dependency flags.</param>
    /// <param name="bufferBarriers">Buffer memory barriers to insert.</param>
    public void PipelineBarrier(PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        DependencyFlags dependencyFlags, ReadOnlySpan<BufferMemoryBarrier> bufferBarriers)
    {
        PipelineBarrier(srcStage, dstStage, dependencyFlags, ReadOnlySpan<MemoryBarrier>.Empty, bufferBarriers,
            ReadOnlySpan<ImageMemoryBarrier>.Empty);
    }

    /// <summary>
    /// Inserts a buffer memory barrier into the command buffer.
    /// </summary>
    /// <param name="srcStage">The source stage of the pipeline.</param>
    /// <param name="dstStage">The destination stage of the pipeline.</param>
    /// <param name="dependencyFlags">Dependency flags.</param>
    /// <param name="bufferBarrier">Buffer memory barrier to insert.</param>
    public void PipelineBarrier(PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        DependencyFlags dependencyFlags, BufferMemoryBarrier bufferBarrier)
    {
        Span<BufferMemoryBarrier> bufferBarriers = [bufferBarrier];

        PipelineBarrier(srcStage, dstStage, dependencyFlags, ReadOnlySpan<MemoryBarrier>.Empty, bufferBarriers,
            ReadOnlySpan<ImageMemoryBarrier>.Empty);
    }

    /// <summary>
    /// Inserts an image memory barrier into the command buffer.
    /// </summary>
    /// <param name="srcStage">The source stage of the pipeline.</param>
    /// <param name="dstStage">The destination stage of the pipeline.</param>
    /// <param name="dependencyFlags">Dependency flags.</param>
    /// <param name="imageBarriers">Image memory barriers to insert.</param>
    public void PipelineBarrier(PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        DependencyFlags dependencyFlags, ReadOnlySpan<ImageMemoryBarrier> imageBarriers)
    {
        PipelineBarrier(srcStage, dstStage, dependencyFlags, ReadOnlySpan<MemoryBarrier>.Empty,
            ReadOnlySpan<BufferMemoryBarrier>.Empty, imageBarriers);
    }

    /// <summary>
    /// Inserts an image memory barrier into the command buffer.
    /// </summary>
    /// <param name="srcStage">The source stage of the pipeline.</param>
    /// <param name="dstStage">The destination stage of the pipeline.</param>
    /// <param name="dependencyFlags">Dependency flags.</param>
    /// <param name="imageBarrier">Image memory barrier to insert.</param>
    public void PipelineBarrier(PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        DependencyFlags dependencyFlags, ImageMemoryBarrier imageBarrier)
    {
        Span<ImageMemoryBarrier> imageBarriers = [imageBarrier];

        PipelineBarrier(srcStage, dstStage, dependencyFlags, ReadOnlySpan<MemoryBarrier>.Empty,
            ReadOnlySpan<BufferMemoryBarrier>.Empty, imageBarriers);
    }

    /// <summary>
    /// Inserts memory, buffer, and/or image barriers into the command buffer.
    /// </summary>
    /// <param name="srcStage">The source stage of the pipeline.</param>
    /// <param name="dstStage">The destination stage of the pipeline.</param>
    /// <param name="dependencyFlags">Dependency flags.</param>
    /// <param name="memoryBarriers">Memory barriers to insert.</param>
    /// <param name="bufferBarriers">Buffer memory barriers to insert.</param>
    /// <param name="imageBarriers">Image memory barriers to insert.</param>
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

    /// <summary>
    /// Copies data from one buffer to another.
    /// </summary>
    /// <param name="src">The source buffer.</param>
    /// <param name="dst">The destination buffer.</param>
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

    /// <summary>
    /// Copies data from one buffer to another, specifying a region to copy.
    /// </summary>
    /// <param name="src">The source buffer.</param>
    /// <param name="dst">The destination buffer.</param>
    /// <param name="region">The region to copy.</param>
    public unsafe void CopyBuffer(MemoryBuffer src, MemoryBuffer dst, BufferCopy region)
    {
        CopyBuffer(src, dst, new ReadOnlySpan<BufferCopy>(&region, 1));
    }

    /// <summary>
    /// Copies data from one buffer to another, specifying multiple regions to copy.
    /// </summary>
    /// <param name="src">The source buffer.</param>
    /// <param name="dst">The destination buffer.</param>
    /// <param name="regions">The regions to copy.</param>
    public void CopyBuffer(MemoryBuffer src, MemoryBuffer dst, ReadOnlySpan<BufferCopy> regions)
    {
        if (State != CommandBufferState.Recording)
            throw new InvalidOperationException("Command buffer must be in recording state to copy a buffer");

        VulkanEngine.Vk.CmdCopyBuffer(InternalCommandBuffer, src.Buffer, dst.Buffer, regions);
    }

    public unsafe void CopyTexture(Texture src, Texture dst)
    {
        var copy = new ImageCopy()
        {
            Extent =
            {
                Depth = src.Depth < dst.Depth ? src.Depth : dst.Depth,
                Height = src.Height < dst.Height ? src.Height : dst.Height,
                Width = src.Width < dst.Width ? src.Width : dst.Width
            },
            DstOffset = default,
            SrcOffset = default,
            DstSubresource =
            {
                AspectMask = FormatHelpers.IsDepthStencilFormat(dst.Format)
                    ? ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit
                    : ImageAspectFlags.ColorBit,
                LayerCount = dst.ArrayLayers,
                BaseArrayLayer = 0,
                MipLevel = 0
            },
            SrcSubresource = new ImageSubresourceLayers()
            {
                AspectMask = FormatHelpers.IsDepthStencilFormat(src.Format)
                    ? ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit
                    : ImageAspectFlags.ColorBit,
                LayerCount = src.ArrayLayers,
                BaseArrayLayer = 0,
                MipLevel = 0
            }
        };

        CopyTexture(src, dst, new ReadOnlySpan<ImageCopy>(&copy, 1));
    }

    public unsafe void CopyTexture(Texture src, Texture dst, ImageCopy region)
    {
        CopyTexture(src, dst, new ReadOnlySpan<ImageCopy>(&region, 1));
    }

    public void CopyTexture(Texture src, Texture dst, ReadOnlySpan<ImageCopy> regions)
    {
        if (!src.HasUniformImageLayout() || !dst.HasUniformImageLayout())
            throw new InvalidOperationException(
                $"Textures must have uniform image layout to be copied with {nameof(ManagedCommandBuffer)}.{nameof(CopyTexture)}");


        var srcLayout = src.GetImageLayout(0, 0);
        var dstLayout = dst.GetImageLayout(0, 0);

        src.TransitionImageLayout(this, 0, src.MipLevels, 0, src.ArrayLayers, ImageLayout.TransferSrcOptimal);
        dst.TransitionImageLayout(this, 0, dst.MipLevels, 0, dst.ArrayLayers, ImageLayout.TransferDstOptimal);
        
        CopyImage(src.Image, dst.Image, ImageLayout.TransferSrcOptimal, ImageLayout.TransferDstOptimal, regions);
        
        src.TransitionImageLayout(this, 0, src.MipLevels, 0, src.ArrayLayers, srcLayout);
        dst.TransitionImageLayout(this, 0, dst.MipLevels, 0, dst.ArrayLayers, dstLayout);
    }

    public void CopyImage(Image srcImage, Image dstImage, ImageLayout srcImageLayout, ImageLayout dstImageLayout,
        ReadOnlySpan<ImageCopy> regions)
    {
        if (State != CommandBufferState.Recording)
            throw new InvalidOperationException("Command buffer must be in recording state to copy a texture");

        VulkanEngine.Vk.CmdCopyImage(InternalCommandBuffer, srcImage, srcImageLayout, dstImage, dstImageLayout,
            regions);
    }
}