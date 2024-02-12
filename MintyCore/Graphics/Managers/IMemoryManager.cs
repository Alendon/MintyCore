using System;
using MintyCore.Graphics.Managers.Implementations;
using MintyCore.Graphics.VulkanObjects;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Graphics.Managers;

/// <summary>
/// Helper class for managing graphics memory
/// </summary>
public interface IMemoryManager
{
    /// <summary>
    ///     Allocate a new Memory Block. Its recommended to directly use either <see cref="MemoryBuffer" /> or
    ///     <see cref="Texture" />
    /// </summary>
    /// <param name="memoryTypeBits">The memory Type bits <see cref="MemoryRequirements.MemoryTypeBits" /></param>
    /// <param name="flags">Memory property flags</param>
    /// <param name="persistentMapped">Should the memory block be persistently mapped</param>
    /// <param name="size">The size of the memory block</param>
    /// <param name="alignment">The alignment of the memory block</param>
    /// <param name="dedicated">Should a dedicated allocation be used</param>
    /// <param name="dedicatedImage"></param>
    /// <param name="dedicatedBuffer"></param>
    /// <param name="addressable"></param>
    /// <returns>Allocated Memory Block</returns>
    MemoryBlock Allocate(
        uint memoryTypeBits,
        MemoryPropertyFlags flags,
        bool persistentMapped,
        ulong size,
        ulong alignment,
        bool dedicated = false,
        Image dedicatedImage = default,
        Buffer dedicatedBuffer = default,
        bool addressable = false);

    /// <summary>
    /// Allocate a new Memory Block and create a buffer
    /// </summary>
    /// <param name="bufferUsage"> Usage of the buffer</param>
    /// <param name="size"> Size of the buffer</param>
    /// <param name="queueFamilyIndices"> The queue family indices, the buffer will be used on</param>
    /// <param name="memoryPropertyFlags"> Memory property flags</param>
    /// <param name="stagingBuffer"> Is the buffer a staging buffer</param>
    /// <param name="sharingMode"> How the buffer is shared</param>
    /// <param name="dedicated"> Should a dedicated allocation be used </param>
    /// <param name="bufferCreateFlags"> Flags for the buffer</param>
    /// <returns> The created buffer</returns>
    /// <remarks>Vulkan only allows a relatively small numbers of dedicated allocations</remarks>
    MemoryBuffer CreateBuffer(BufferUsageFlags bufferUsage, ulong size,
        Span<uint> queueFamilyIndices, MemoryPropertyFlags memoryPropertyFlags, bool stagingBuffer,
        SharingMode sharingMode = SharingMode.Exclusive,
        bool dedicated = false, BufferCreateFlags bufferCreateFlags = 0);

    /// <summary>
    ///     Free a memory block
    /// </summary>
    /// <param name="block">To free</param>
    void Free(MemoryBlock block);

    /// <summary>
    ///     Clear the Memory Manager
    /// </summary>
    void Clear();

    /// <summary>
    ///     Map a memory block
    /// </summary>
    /// <param name="memoryBlock">to map</param>
    /// <returns><see cref="IntPtr" /> to the data</returns>
    IntPtr Map(MemoryBlock memoryBlock);

    /// <summary>
    ///     Unmap a memory block
    /// </summary>
    /// <param name="memoryBlock">to unmap</param>
    void UnMap(MemoryBlock memoryBlock);
}