using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

/// <summary>
///     Helper class for various vulkan functions
/// </summary>
[PublicAPI]
public static unsafe class VulkanUtils
{
    /// <summary>
    ///     Compute the offset of a subresource
    /// </summary>
    /// <param name="tex">The texture to calculate the subresource</param>
    /// <param name="mipLevel">The mip level of the subresource</param>
    /// <param name="arrayLayer">The array layer of the subresource</param>
    /// <returns>Offset</returns>
    public static ulong ComputeSubresourceOffset(Texture tex, uint mipLevel, uint arrayLayer)
    {
        Debug.Assert((tex.Usage & TextureUsage.Staging) == TextureUsage.Staging);
        return ComputeArrayLayerOffset(tex, arrayLayer) + ComputeMipOffset(tex, mipLevel);
    }

    private static uint ComputeMipOffset(Texture tex, uint mipLevel)
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

    private static uint ComputeArrayLayerOffset(Texture tex, uint arrayLayer)
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

    /// <summary>
    ///     Get mip level and array layer of subresource
    /// </summary>
    /// <param name="tex"></param>
    /// <param name="subresource"></param>
    /// <param name="mipLevel"></param>
    /// <param name="arrayLayer"></param>
    public static void GetMipLevelAndArrayLayer(Texture tex, uint subresource, out uint mipLevel,
        out uint arrayLayer)
    {
        arrayLayer = subresource / tex.MipLevels;
        mipLevel = subresource - arrayLayer * tex.MipLevels;
    }

    /// <summary>
    /// </summary>
    /// <param name="cb"></param>
    /// <param name="image"></param>
    /// <param name="baseMipLevel"></param>
    /// <param name="levelCount"></param>
    /// <param name="baseArrayLayer"></param>
    /// <param name="layerCount"></param>
    /// <param name="aspectMask"></param>
    /// <param name="oldLayout"></param>
    /// <param name="newLayout"></param>
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
                LayerCount = layerCount
            }
        };

        PipelineStageFlags srcStageFlags = 0;
        PipelineStageFlags dstStageFlags = 0;

        switch (oldLayout)
        {
            case ImageLayout.Undefined or ImageLayout.Preinitialized when newLayout == ImageLayout.TransferDstOptimal:
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                srcStageFlags = PipelineStageFlags.TopOfPipeBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.ShaderReadOnlyOptimal when newLayout == ImageLayout.TransferSrcOptimal:
                barrier.SrcAccessMask = AccessFlags.ShaderReadBit;
                barrier.DstAccessMask = AccessFlags.TransferReadBit;
                srcStageFlags = PipelineStageFlags.FragmentShaderBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.ShaderReadOnlyOptimal when newLayout == ImageLayout.TransferDstOptimal:
                barrier.SrcAccessMask = AccessFlags.ShaderReadBit;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                srcStageFlags = PipelineStageFlags.FragmentShaderBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.Preinitialized when newLayout == ImageLayout.TransferSrcOptimal:
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.TransferReadBit;
                srcStageFlags = PipelineStageFlags.TopOfPipeBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.Preinitialized when newLayout == ImageLayout.General:
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.TopOfPipeBit;
                dstStageFlags = PipelineStageFlags.ComputeShaderBit;
                break;
            case ImageLayout.Preinitialized when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.TopOfPipeBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.General when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferReadBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.ShaderReadOnlyOptimal when newLayout == ImageLayout.General:
                barrier.SrcAccessMask = AccessFlags.ShaderReadBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.FragmentShaderBit;
                dstStageFlags = PipelineStageFlags.ComputeShaderBit;
                break;
            case ImageLayout.TransferSrcOptimal when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferReadBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.TransferDstOptimal when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.TransferSrcOptimal when newLayout == ImageLayout.TransferDstOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferReadBit;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.TransferDstOptimal when newLayout == ImageLayout.TransferSrcOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.TransferReadBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.ColorAttachmentOptimal when newLayout == ImageLayout.TransferSrcOptimal:
                barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                barrier.DstAccessMask = AccessFlags.TransferReadBit;
                srcStageFlags = PipelineStageFlags.ColorAttachmentOutputBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.ColorAttachmentOptimal when newLayout == ImageLayout.TransferDstOptimal:
                barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                srcStageFlags = PipelineStageFlags.ColorAttachmentOutputBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.ColorAttachmentOptimal when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.ColorAttachmentOutputBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.DepthStencilAttachmentOptimal when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = AccessFlags.DepthStencilAttachmentWriteBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.LateFragmentTestsBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.ColorAttachmentOptimal when newLayout == ImageLayout.PresentSrcKhr:
                barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                barrier.DstAccessMask = AccessFlags.MemoryReadBit;
                srcStageFlags = PipelineStageFlags.ColorAttachmentOutputBit;
                dstStageFlags = PipelineStageFlags.BottomOfPipeBit;
                break;
            case ImageLayout.TransferDstOptimal when newLayout == ImageLayout.PresentSrcKhr:
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.MemoryReadBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.BottomOfPipeBit;
                break;
            case ImageLayout.TransferDstOptimal when newLayout == ImageLayout.ColorAttachmentOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.ColorAttachmentWriteBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.ColorAttachmentOutputBit;
                break;
            case ImageLayout.TransferDstOptimal when newLayout == ImageLayout.DepthStencilAttachmentOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.DepthStencilAttachmentWriteBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.LateFragmentTestsBit;
                break;
            default:
                Debug.Fail("Invalid image layout transition.");
                break;
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

    /// <summary>
    ///     Enumerate device extensions
    /// </summary>
    /// <param name="device">Device to enumerate</param>
    /// <param name="layer">Optional to get layer information</param>
    /// <returns>Available extensions</returns>
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
                extensionNames[i] = Marshal.PtrToStringAnsi((IntPtr) name) ?? string.Empty;
            }

        return extensionNames;
    }

    /// <summary>
    /// </summary>
    /// <param name="layerName"></param>
    /// <returns></returns>
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
                extensionNames[i] = Marshal.PtrToStringAnsi((IntPtr) name) ?? string.Empty;
            }

        return extensionNames;
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
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
                layerNames[i] = Marshal.PtrToStringAnsi((IntPtr) name) ?? string.Empty;
            }

        return layerNames;
    }

    /// <summary>
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    public static PhysicalDevice[] EnumerateDevices(Instance instance)
    {
        uint deviceCount = 0;
        Assert(VulkanEngine.Vk.EnumeratePhysicalDevices(instance, ref deviceCount, null));
        if (deviceCount == 0) return Array.Empty<PhysicalDevice>();

        var devices = new PhysicalDevice[deviceCount];
        Assert(VulkanEngine.Vk.EnumeratePhysicalDevices(instance, ref deviceCount, ref devices[0]));
        return devices;
    }

    /// <summary>
    /// </summary>
    /// <param name="typeFilter"></param>
    /// <param name="requiredFlags"></param>
    /// <param name="memoryTypeIndex"></param>
    /// <returns></returns>
    public static bool FindMemoryType(uint typeFilter, MemoryPropertyFlags requiredFlags, out uint memoryTypeIndex)
    {
        for (var i = 0; i < VulkanEngine.PhysicalDeviceMemoryProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << i)) == 0 ||
                (VulkanEngine.PhysicalDeviceMemoryProperties.MemoryTypes[i].PropertyFlags & requiredFlags) !=
                requiredFlags) continue;

            memoryTypeIndex = (uint) i;
            return true;
        }

        memoryTypeIndex = uint.MaxValue;
        return false;
    }

    /// <summary>
    /// </summary>
    public static void GetMipDimensions(uint texWidth, uint texHeight, uint texDepth, uint mipLevel, out uint width,
        out uint height, out uint depth)
    {
        width = GetDimension(texWidth, mipLevel);
        height = GetDimension(texHeight, mipLevel);
        depth = GetDimension(texDepth, mipLevel);
    }

    /// <summary>
    /// </summary>
    public static void GetMipDimensions(Texture tex, uint mipLevel, out uint width, out uint height, out uint depth)
    {
        width = GetDimension(tex.Width, mipLevel);
        height = GetDimension(tex.Height, mipLevel);
        depth = GetDimension(tex.Depth, mipLevel);
    }

    /// <summary>
    /// </summary>
    /// <param name="largestLevelDimension"></param>
    /// <param name="mipLevel"></param>
    /// <returns></returns>
    public static uint GetDimension(uint largestLevelDimension, uint mipLevel)
    {
        var ret = largestLevelDimension;
        for (uint i = 0; i < mipLevel; i++) ret /= 2;

        return Math.Max(1, ret);
    }

    /// <summary>
    ///     Assert the vulkan result. Throws error if no success
    /// </summary>
    /// <param name="result">Result of a vulkan operation</param>
    /// <exception cref="VulkanException">result != <see cref="Result.Success" /></exception>
    public static void Assert(Result result)
    {
        Logger.AssertAndThrow(result == Result.Success, $"Vulkan Execution Failed:  {result}", "Render");
    }

    /// <summary>
    /// Check if the vulkan instance is valid
    /// <exception cref="MintyCoreException">No valid vulkan instance is available</exception>
    /// </summary>
    public static void AssertVulkanInstance()
    {
        Logger.AssertAndThrow(VulkanEngine.Device.Handle != default, "No valid vulkan instance", "Render");
    }
}

/// <summary>
///     Exception for vulkan errors
/// </summary>
public class VulkanException : Exception
{
    /// <summary>
    /// </summary>
    /// <param name="result"></param>
    public VulkanException(Result result) : base($"A Vulkan Exception occured({result})")
    {
    }

    /// <summary>
    /// </summary>
    /// <param name="message"></param>
    public VulkanException(string message) : base(message)
    {
    }
}

/// <summary>
///     Struct containing queue family indexes
/// </summary>
public struct QueueFamilyIndexes
{
    /// <summary>
    ///     Index of graphics family
    /// </summary>
    public uint? GraphicsFamily;

    /// <summary>
    ///     Index of present family
    /// </summary>
    public uint? PresentFamily;

    /// <summary>
    ///     Index of compute family
    /// </summary>
    public uint? ComputeFamily;
}