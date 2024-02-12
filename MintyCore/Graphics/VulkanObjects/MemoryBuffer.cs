using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Managers.Implementations;
using MintyCore.Utils;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Graphics.VulkanObjects;

/// <summary>
///     Struct containing a vulkan buffer with associated memory
/// </summary>
[PublicAPI]
public sealed class MemoryBuffer : VulkanObject
{
    /// <summary>
    ///     A memory block.
    ///     <seealso cref="_memoryManager" />
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
    ///   Whether the memory block should be disposed alongside the object
    /// </summary>
    public readonly bool DisposeMemoryBlock;

    private readonly IMemoryManager _memoryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryBuffer"/> class.
    /// </summary>
    /// <param name="vulkanEngine"> The vulkan engine. </param>
    /// <param name="allocationHandler"> The allocation handler. </param>
    /// <param name="memoryManager"> The memory manager. </param>
    /// <param name="memoryBlock"> The memory block. </param>
    /// <param name="buffer"> The buffer. </param>
    /// <param name="size"> The size. </param>
    /// <param name="disposeMemoryBlock"> Whether the memory block should be disposed alongside the object. </param>
    public MemoryBuffer(IVulkanEngine vulkanEngine, IAllocationHandler allocationHandler, IMemoryManager memoryManager,
        MemoryBlock memoryBlock, Buffer buffer, ulong size, bool disposeMemoryBlock = true) : base(vulkanEngine,
        allocationHandler)
    {
        Memory = memoryBlock;
        Buffer = buffer;
        Size = size;
        _memoryManager = memoryManager;
        DisposeMemoryBlock = disposeMemoryBlock;
    }

    /// <inheritdoc />
    protected override unsafe void ReleaseUnmanagedResources()
    {
        if (DisposeMemoryBlock)
        {
            _memoryManager.Free(Memory);
        }

        VulkanEngine.Vk.DestroyBuffer(VulkanEngine.Device, Buffer, null);
    }

    /// <summary>
    /// Maps the memory block as a span of T
    /// </summary>
    /// <typeparam name="T"> The type of the span. </typeparam>
    /// <returns> A span of T </returns>
    /// <exception cref="InvalidOperationException"> Size of T is greater than the size of the buffer </exception>
    public unsafe Span<T> MapAs<T>() where T : unmanaged
    {
        if (Unsafe.SizeOf<T>() > (int)Size)
            throw new InvalidOperationException("Size of T is greater than the size of the buffer");

        var ptr = _memoryManager.Map(Memory);
        return new Span<T>(ptr.ToPointer(), (int)Size / Unsafe.SizeOf<T>());
    }

    /// <summary>
    ///   Unmaps the memory block
    /// </summary>
    public void Unmap()
    {
        _memoryManager.UnMap(Memory);
    }
}