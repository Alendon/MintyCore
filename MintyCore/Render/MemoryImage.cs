using System;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render
{
    public readonly struct MemoryImage : IDisposable
    {
        public readonly DeviceMemory Memory;
        public readonly Image Image;
        public readonly ulong Size;

        public MemoryImage(DeviceMemory memory, Image image, ulong size)
        {
            Memory = memory;
            Image = image;
            Size = size;
        }

        public unsafe void* MapMemory(uint arrayLayer, uint MipLevel, ImageAspectFlags imageAspectFlags)
        {
            void* data = null;

            VulkanUtils.Assert(VulkanEngine.Vk.MapMemory(VulkanEngine.Device, Memory, 0, Size, 0, ref data));
            return data;
        }

        public void UnMap()
        {
            VulkanEngine.Vk.UnmapMemory(VulkanEngine.Device, Memory);
        }

        public static unsafe MemoryImage Create(Format format, Extent3D extent, Span<uint> queueFamilies,
            ImageUsageFlags usage, SharingMode sharingMode, SampleCountFlags samples, uint mipLevels,
            ImageTiling tiling, ImageLayout initialImageLayout, MemoryPropertyFlags memoryPropertyFlags,
            uint arrayLayers = 1,
            ImageType imageType = ImageType.ImageType2D, ImageCreateFlags flags = 0)
        {
            Image image;
            fixed (uint* queues = &queueFamilies.GetPinnableReference())
            {
                ImageCreateInfo createInfo = new()
                {
                    SType = StructureType.ImageCreateInfo,
                    PNext = null,
                    Format = format,
                    Extent = extent,
                    Flags = flags,
                    Samples = samples,
                    Tiling = tiling,
                    Usage = usage,
                    ImageType = imageType,
                    InitialLayout = initialImageLayout,
                    SharingMode = sharingMode,
                    PQueueFamilyIndices = queues,
                    QueueFamilyIndexCount = (uint)queueFamilies.Length,
                    ArrayLayers = arrayLayers,
                    MipLevels = mipLevels
                };

                VulkanUtils.Assert(VulkanEngine.Vk.CreateImage(VulkanEngine.Device, createInfo,
                    VulkanEngine.AllocationCallback, out image));
            }

            VulkanEngine.Vk.GetImageMemoryRequirements(VulkanEngine.Device, image, out var requirements);

            if (!VulkanUtils.FindMemoryType(requirements.MemoryTypeBits, memoryPropertyFlags, out var index))
            {
                memoryPropertyFlags ^= MemoryPropertyFlags.MemoryPropertyHostCachedBit;
                if (!VulkanUtils.FindMemoryType(requirements.MemoryTypeBits, memoryPropertyFlags, out index))
                    Logger.WriteLog("Couldnt find required memory type", LogImportance.EXCEPTION, "Render");
            }

            MemoryAllocateInfo allocateInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = requirements.Size,
                MemoryTypeIndex = (uint)index
            };

            VulkanUtils.Assert(VulkanEngine.Vk.AllocateMemory(VulkanEngine.Device, allocateInfo,
                VulkanEngine.AllocationCallback, out var memory));
            VulkanUtils.Assert(VulkanEngine.Vk.BindImageMemory(VulkanEngine.Device, image, memory, 0));

            return new MemoryImage(memory, image, requirements.Size);
        }

        public unsafe void Dispose()
        {
            VulkanEngine.Vk.FreeMemory(VulkanEngine.Device, Memory, VulkanEngine.AllocationCallback);
            VulkanEngine.Vk.DestroyImage(VulkanEngine.Device, Image, VulkanEngine.AllocationCallback);
        }
    }
}