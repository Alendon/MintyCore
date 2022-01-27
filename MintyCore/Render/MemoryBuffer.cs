using System;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using static MintyCore.Render.VulkanEngine;
using static MintyCore.Render.VulkanUtils;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Render;

public readonly struct MemoryBuffer : IDisposable
{
    public readonly DeviceMemory Memory;
    public readonly Buffer Buffer;
    public readonly ulong Size;

    public unsafe void* MapMemory(ulong offset = 0)
    {
        void* data = null;
        Assert(VulkanEngine.Vk.MapMemory(VulkanEngine.Device, Memory, 0, Size, 0, ref data));
        return data;
    }

    public void UnMap()
    {
        VulkanEngine.Vk.UnmapMemory(VulkanEngine.Device, Memory);
    }

    public static unsafe MemoryBuffer Create(BufferUsageFlags bufferUsage, ulong size, SharingMode sharingMode,
        Span<uint> QueueFamilyIndices, MemoryPropertyFlags memoryPropertyFlags,
        BufferCreateFlags bufferCreateFlags = 0)
    {
        Buffer buffer;
        fixed (uint* queueFamilyIndex = &QueueFamilyIndices[0])
        {
            BufferCreateInfo createInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Flags = bufferCreateFlags,
                Size = size,
                Usage = bufferUsage,
                SharingMode = sharingMode,
                QueueFamilyIndexCount = (uint)QueueFamilyIndices.Length,
                PQueueFamilyIndices = queueFamilyIndex
            };
            Assert(VulkanEngine.Vk.CreateBuffer(VulkanEngine.Device, createInfo, AllocationCallback, out buffer));
        }

        MemoryRequirements memoryRequirements;
        VulkanEngine.Vk.GetBufferMemoryRequirements(VulkanEngine.Device, buffer, out memoryRequirements);
        if (!FindMemoryType(memoryRequirements.MemoryTypeBits,
                memoryPropertyFlags,
                out var memoryTypeIndex))
            Logger.WriteLog("Couldnt find required memory type", LogImportance.EXCEPTION, "Render");

        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memoryRequirements.Size,
            MemoryTypeIndex = memoryTypeIndex
        };

        Assert(
            VulkanEngine.Vk.AllocateMemory(VulkanEngine.Device, allocateInfo, AllocationCallback, out var memory));
        Assert(VulkanEngine.Vk.BindBufferMemory(VulkanEngine.Device, buffer, memory, 0));

        return new MemoryBuffer(memory, buffer, size);
    }


    private MemoryBuffer(DeviceMemory memory, Buffer buffer, ulong size)
    {
        Memory = memory;
        Buffer = buffer;
        Size = size;
    }

    public unsafe void Dispose()
    {
        VulkanEngine.Vk.FreeMemory(VulkanEngine.Device, Memory, AllocationCallback);
        VulkanEngine.Vk.DestroyBuffer(VulkanEngine.Device, Buffer, AllocationCallback);
    }
}