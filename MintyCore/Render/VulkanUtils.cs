using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace MintyCore.Render;

public static unsafe class VulkanUtils
{
    public static ulong ComputeSubresourceOffset(Texture tex, uint mipLevel, uint arrayLayer)
    {
        Debug.Assert((tex.Usage & TextureUsage.STAGING) == TextureUsage.STAGING);
        return ComputeArrayLayerOffset(tex, arrayLayer) + ComputeMipOffset(tex, mipLevel);
    }

    internal static uint ComputeMipOffset(Texture tex, uint mipLevel)
    {
        var blockSize = FormatHelpers.IsCompressedFormat(tex.Format) ? 4u : 1u;
        uint offset = 0;
        for (uint level = 0; level < mipLevel; level++)
        {
            GetMipDimensions(tex, level, out var mipWidth, out var mipHeight, out var mipDepth);
            var storageWidth = Math.Max(mipWidth, blockSize);
            var storageHeight = Math.Max(mipHeight, blockSize);
            offset += FormatHelpers.GetRegionSize(storageWidth, storageHeight, mipDepth, tex.Format);
        }

        return offset;
    }

    internal static uint ComputeArrayLayerOffset(Texture tex, uint arrayLayer)
    {
        if (arrayLayer == 0) return 0;

        var blockSize = FormatHelpers.IsCompressedFormat(tex.Format) ? 4u : 1u;
        uint layerPitch = 0;
        for (uint level = 0; level < tex.MipLevels; level++)
        {
            GetMipDimensions(tex, level, out var mipWidth, out var mipHeight, out var mipDepth);
            var storageWidth = Math.Max(mipWidth, blockSize);
            var storageHeight = Math.Max(mipHeight, blockSize);
            layerPitch += FormatHelpers.GetRegionSize(storageWidth, storageHeight, mipDepth, tex.Format);
        }

        return layerPitch * arrayLayer;
    }

    public static void GetMipLevelAndArrayLayer(Texture tex, uint subresource, out uint mipLevel,
        out uint arrayLayer)
    {
        arrayLayer = subresource / tex.MipLevels;
        mipLevel = subresource - arrayLayer * tex.MipLevels;
    }

    public static void TransitionImageLayout(
        CommandBuffer cb,
        Image image,
        uint baseMipLevel,
        uint levelCount,
        uint baseArrayLayer,
        uint layerCount,
        ImageAspectFlags aspectMask,
        ImageLayout oldLayout,
        ImageLayout newLayout)
    {
        Debug.Assert(oldLayout != newLayout);
        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange =
            {
                AspectMask = aspectMask, BaseMipLevel = baseMipLevel,
                LevelCount = levelCount,
                BaseArrayLayer = baseArrayLayer,
                LayerCount = layerCount,
            }
        };

        PipelineStageFlags srcStageFlags = 0;
        PipelineStageFlags dstStageFlags = 0;

        if ((oldLayout == ImageLayout.Undefined || oldLayout == ImageLayout.Preinitialized) &&
            newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.AccessTransferWriteBit;
            srcStageFlags = PipelineStageFlags.PipelineStageTopOfPipeBit;
            dstStageFlags = PipelineStageFlags.PipelineStageTransferBit;
        }
        else if (oldLayout == ImageLayout.ShaderReadOnlyOptimal && newLayout == ImageLayout.TransferSrcOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessShaderReadBit;
            barrier.DstAccessMask = AccessFlags.AccessTransferReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageFragmentShaderBit;
            dstStageFlags = PipelineStageFlags.PipelineStageTransferBit;
        }
        else if (oldLayout == ImageLayout.ShaderReadOnlyOptimal && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessShaderReadBit;
            barrier.DstAccessMask = AccessFlags.AccessTransferWriteBit;
            ;
            srcStageFlags = PipelineStageFlags.PipelineStageFragmentShaderBit;
            dstStageFlags = PipelineStageFlags.PipelineStageTransferBit;
        }
        else if (oldLayout == ImageLayout.Preinitialized && newLayout == ImageLayout.TransferSrcOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.AccessTransferReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageTopOfPipeBit;
            dstStageFlags = PipelineStageFlags.PipelineStageTransferBit;
        }
        else if (oldLayout == ImageLayout.Preinitialized && newLayout == ImageLayout.General)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageTopOfPipeBit;
            dstStageFlags = PipelineStageFlags.PipelineStageComputeShaderBit;
        }
        else if (oldLayout == ImageLayout.Preinitialized && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageTopOfPipeBit;
            dstStageFlags = PipelineStageFlags.PipelineStageFragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.General && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessTransferReadBit;
            barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageTransferBit;
            dstStageFlags = PipelineStageFlags.PipelineStageFragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.ShaderReadOnlyOptimal && newLayout == ImageLayout.General)
        {
            barrier.SrcAccessMask = AccessFlags.AccessShaderReadBit;
            barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageFragmentShaderBit;
            dstStageFlags = PipelineStageFlags.PipelineStageComputeShaderBit;
        }

        else if (oldLayout == ImageLayout.TransferSrcOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessTransferReadBit;
            barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageTransferBit;
            dstStageFlags = PipelineStageFlags.PipelineStageFragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessTransferWriteBit;
            ;
            barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageTransferBit;
            dstStageFlags = PipelineStageFlags.PipelineStageFragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.TransferSrcOptimal && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessTransferReadBit;
            barrier.DstAccessMask = AccessFlags.AccessTransferWriteBit;
            ;
            srcStageFlags = PipelineStageFlags.PipelineStageTransferBit;
            dstStageFlags = PipelineStageFlags.PipelineStageTransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.TransferSrcOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessTransferWriteBit;
            ;
            barrier.DstAccessMask = AccessFlags.AccessTransferReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageTransferBit;
            dstStageFlags = PipelineStageFlags.PipelineStageTransferBit;
        }
        else if (oldLayout == ImageLayout.ColorAttachmentOptimal && newLayout == ImageLayout.TransferSrcOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessColorAttachmentWriteBit;
            barrier.DstAccessMask = AccessFlags.AccessTransferReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
            dstStageFlags = PipelineStageFlags.PipelineStageTransferBit;
        }
        else if (oldLayout == ImageLayout.ColorAttachmentOptimal && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessColorAttachmentWriteBit;
            barrier.DstAccessMask = AccessFlags.AccessTransferWriteBit;
            ;
            srcStageFlags = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
            dstStageFlags = PipelineStageFlags.PipelineStageTransferBit;
        }
        else if (oldLayout == ImageLayout.ColorAttachmentOptimal &&
                 newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessColorAttachmentWriteBit;
            barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
            dstStageFlags = PipelineStageFlags.PipelineStageFragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.DepthStencilAttachmentOptimal &&
                 newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessDepthStencilAttachmentWriteBit;
            barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageLateFragmentTestsBit;
            dstStageFlags = PipelineStageFlags.PipelineStageFragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.ColorAttachmentOptimal && newLayout == ImageLayout.PresentSrcKhr)
        {
            barrier.SrcAccessMask = AccessFlags.AccessColorAttachmentWriteBit;
            barrier.DstAccessMask = AccessFlags.AccessMemoryReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
            dstStageFlags = PipelineStageFlags.PipelineStageBottomOfPipeBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.PresentSrcKhr)
        {
            barrier.SrcAccessMask = AccessFlags.AccessTransferWriteBit;
            ;
            barrier.DstAccessMask = AccessFlags.AccessMemoryReadBit;
            srcStageFlags = PipelineStageFlags.PipelineStageTransferBit;
            dstStageFlags = PipelineStageFlags.PipelineStageBottomOfPipeBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ColorAttachmentOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessTransferWriteBit;
            ;
            barrier.DstAccessMask = AccessFlags.AccessColorAttachmentWriteBit;
            srcStageFlags = PipelineStageFlags.PipelineStageTransferBit;
            dstStageFlags = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal &&
                 newLayout == ImageLayout.DepthStencilAttachmentOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.AccessTransferWriteBit;
            ;
            barrier.DstAccessMask = AccessFlags.AccessDepthStencilAttachmentWriteBit;
            srcStageFlags = PipelineStageFlags.PipelineStageTransferBit;
            dstStageFlags = PipelineStageFlags.PipelineStageLateFragmentTestsBit;
        }
        else
        {
            Debug.Fail("Invalid image layout transition.");
        }

        VulkanEngine.Vk.CmdPipelineBarrier(
            cb,
            srcStageFlags,
            dstStageFlags,
            0,
            0, null,
            0, null,
            1, &barrier);
    }


    public static PresentInfoKHR GetPresentInfo(SwapchainKHR* swapchains, uint swapchainCount,
        VkSemaphore* waitSemaphores, uint waitSemaphoreCount, uint* imageIndices, Result* results = null,
        void* pNext = null)
    {
        return new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            PWaitSemaphores = waitSemaphores,
            WaitSemaphoreCount = waitSemaphoreCount,
            PSwapchains = swapchains,
            SwapchainCount = swapchainCount,
            PImageIndices = imageIndices,
            PResults = results,
            PNext = pNext
        };
    }

    public static SubmitInfo GetSubmitInfo(CommandBuffer* commandBuffers, uint commandBufferCount,
        VkSemaphore* waitSemaphores, uint waitSemaphoreCount, VkSemaphore* signalSemaphore,
        uint signalSemaphoreCount, PipelineStageFlags* waitStageMask, void* pNext = null)
    {
        return new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            PNext = pNext,
            CommandBufferCount = commandBufferCount,
            PCommandBuffers = commandBuffers,
            PSignalSemaphores = signalSemaphore,
            PWaitSemaphores = waitSemaphores,
            SignalSemaphoreCount = signalSemaphoreCount,
            WaitSemaphoreCount = waitSemaphoreCount,
            PWaitDstStageMask = waitStageMask
        };
    }

    public static string[] EnumerateDeviceExtensions(PhysicalDevice device, byte* layer = null)
    {
        uint extensionCount = 0;
        VulkanEngine.Vk.EnumerateDeviceExtensionProperties(device, layer, ref extensionCount, null);
        var properties = new ExtensionProperties[extensionCount];
        VulkanEngine.Vk.EnumerateDeviceExtensionProperties(device, layer, ref extensionCount, ref properties[0]);
        var extensionNames = new string[extensionCount];
        for (var i = 0; i < extensionCount; i++)
            fixed (byte* name = properties[i].ExtensionName)
            {
                extensionNames[i] = Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
            }

        return extensionNames;
    }

    public static string[] EnumerateDeviceLayers(PhysicalDevice device)
    {
        uint layerCount = 0;
        VulkanEngine.Vk.EnumerateDeviceLayerProperties(device, ref layerCount, null);
        var properties = new LayerProperties[layerCount];
        VulkanEngine.Vk.EnumerateDeviceLayerProperties(device, ref layerCount, ref properties[0]);
        var layerNames = new string[layerCount];
        for (var i = 0; i < layerCount; i++)
            fixed (byte* name = properties[i].LayerName)
            {
                layerNames[i] = Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
            }

        return layerNames;
    }

    public static string[] EnumerateInstanceExtensions(byte* layerName = null)
    {
        uint extensionCount = 0;
        VulkanEngine.Vk.EnumerateInstanceExtensionProperties(layerName, ref extensionCount, null);
        var properties = new ExtensionProperties[extensionCount];
        VulkanEngine.Vk.EnumerateInstanceExtensionProperties(layerName, ref extensionCount, ref properties[0]);
        var extensionNames = new string[extensionCount];
        for (var i = 0; i < extensionCount; i++)
            fixed (byte* name = properties[i].ExtensionName)
            {
                extensionNames[i] = Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
            }

        return extensionNames;
    }

    public static string[] EnumerateInstanceLayers()
    {
        uint layerCount = 0;
        VulkanEngine.Vk.EnumerateInstanceLayerProperties(ref layerCount, null);
        var properties = new LayerProperties[layerCount];
        VulkanEngine.Vk.EnumerateInstanceLayerProperties(ref layerCount, ref properties[0]);
        var layerNames = new string[layerCount];
        for (var i = 0; i < layerCount; i++)
            fixed (byte* name = properties[i].LayerName)
            {
                layerNames[i] = Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
            }

        return layerNames;
    }

    public static PhysicalDevice[] EnumerateDevices(Instance instance)
    {
        uint deviceCount = 0;
        Assert(VulkanEngine.Vk.EnumeratePhysicalDevices(instance, ref deviceCount, null));
        if (deviceCount == 0) return Array.Empty<PhysicalDevice>();

        var devices = new PhysicalDevice[deviceCount];
        Assert(VulkanEngine.Vk.EnumeratePhysicalDevices(instance, ref deviceCount, ref devices[0]));
        return devices;
    }


    public static bool FindMemoryType(uint typeFilter, MemoryPropertyFlags requiredFlags, out uint memoryTypeIndex)
    {
        for (var i = 0; i < VulkanEngine.PhysicalDeviceMemoryProperties.MemoryTypeCount; i++)
        {
            var flagged = typeFilter & (1u << i);
            if ((typeFilter & (1u << i)) != 0 &&
                (VulkanEngine.PhysicalDeviceMemoryProperties.MemoryTypes[i].PropertyFlags & requiredFlags) ==
                requiredFlags)
            {
                memoryTypeIndex = (uint)i;
                return true;
            }
        }

        memoryTypeIndex = uint.MaxValue;
        return false;
    }

    public static void GetMipDimensions(Texture tex, uint mipLevel, out uint width, out uint height, out uint depth)
    {
        width = GetDimension(tex.Width, mipLevel);
        height = GetDimension(tex.Height, mipLevel);
        depth = GetDimension(tex.Depth, mipLevel);
    }

    public static uint GetDimension(uint largestLevelDimension, uint mipLevel)
    {
        var ret = largestLevelDimension;
        for (uint i = 0; i < mipLevel; i++) ret /= 2;

        return Math.Max(1, ret);
    }

    public static void Assert(Result result)
    {
        if (result != Result.Success) throw new VulkanException(result);
    }
}

class VulkanException : Exception
{
    public VulkanException(Result result) : base($"A Vulkan Exception occured({result})")
    {
    }

    public VulkanException(string message) : base(message)
    {
    }
}

public struct QueueFamilyIndexes
{
    public uint? GraphicsFamily;
    public uint? PresentFamily;
    public uint? ComputeFamily;
}