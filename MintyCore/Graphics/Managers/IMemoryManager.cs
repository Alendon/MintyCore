using System;
using MintyCore.Graphics.Managers.Implementations;
using MintyCore.Graphics.VulkanObjects;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Graphics.Managers;

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
    /// <returns>Allocated Memory Block</returns>
    MemoryBlock Allocate(
        uint memoryTypeBits,
        MemoryPropertyFlags flags,
        bool persistentMapped,
        ulong size,
        ulong alignment,
        bool dedicated = true,
        Image dedicatedImage = default,
        Buffer dedicatedBuffer = default,
        bool addressable = false);

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