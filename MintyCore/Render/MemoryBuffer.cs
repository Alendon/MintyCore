using System;
using Silk.NET.Vulkan;
using static MintyCore.Render.VulkanEngine;
using static MintyCore.Render.VulkanUtils;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Render;

/// <summary>
///     Struct containing a vulkan buffer with associated memory
/// </summary>
public readonly struct MemoryBuffer : IDisposable
{
    /// <summary>
    ///     A memory block.
    ///     <seealso cref="MemoryManager" />
    /// </summary>
    public readonly MemoryBlock Memory;

    /// <summary>
    ///     A vulkan buffer
    /// </summary>
    public readonly Buffer Buffer;

    /// <summary>
    ///     The size of the buffer
    /// </summary>
    public readonly ulong Size;

    /// <summary>
    ///     Create a new Memory buffer
    /// </summary>
    /// <param name="bufferUsage">The usage of the buffer</param>
    /// <param name="size">The size of the buffer</param>
    /// <param name="sharingMode">The sharing mode between multiple queues</param>
    /// <param name="queueFamilyIndices">The queue families the buffer has to be available from</param>
    /// <param name="memoryPropertyFlags">The memory properties of the buffer</param>
    /// <param name="stagingBuffer">Whether or not the buffer is only for staging (persistently mapped on the cpu)</param>
    /// <param name="bufferCreateFlags">Optional create flags for the buffer</param>
    /// <returns>Created Memory Buffer</returns>
    public static unsafe MemoryBuffer Create(BufferUsageFlags bufferUsage, ulong size, SharingMode sharingMode,
        Span<uint> queueFamilyIndices, MemoryPropertyFlags memoryPropertyFlags, bool stagingBuffer,
        BufferCreateFlags bufferCreateFlags = 0)
    {
        Buffer buffer;
        fixed (uint* queueFamilyIndex = &queueFamilyIndices[0])
        {
            BufferCreateInfo createInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Flags = bufferCreateFlags,
                Size = size,
                Usage = bufferUsage,
                SharingMode = sharingMode,
                QueueFamilyIndexCount = (uint) queueFamilyIndices.Length,
                PQueueFamilyIndices = queueFamilyIndex
            };
            Assert(VulkanEngine.Vk.CreateBuffer(VulkanEngine.Device, createInfo, AllocationCallback, out buffer));
        }

        VulkanEngine.Vk.GetBufferMemoryRequirements(VulkanEngine.Device, buffer, out var memoryRequirements);
        var memory = MemoryManager.Allocate(memoryRequirements.MemoryTypeBits, memoryPropertyFlags, stagingBuffer,
            memoryRequirements.Size, memoryRequirements.Alignment, true, default, buffer);


        Assert(VulkanEngine.Vk.BindBufferMemory(VulkanEngine.Device, buffer, memory.DeviceMemory, 0));

        return new MemoryBuffer(memory, buffer, size);
    }


    private MemoryBuffer(MemoryBlock memory, Buffer buffer, ulong size)
    {
        Memory = memory;
        Buffer = buffer;
        Size = size;
    }

    /// <inheritdoc />
    public unsafe void Dispose()
    {
        MemoryManager.Free(Memory);
        VulkanEngine.Vk.DestroyBuffer(VulkanEngine.Device, Buffer, AllocationCallback);
    }
}