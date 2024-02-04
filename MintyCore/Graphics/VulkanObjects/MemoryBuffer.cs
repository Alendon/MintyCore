using System;
using System.Runtime.CompilerServices;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Managers.Implementations;
using MintyCore.Utils;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Graphics.VulkanObjects;

/// <summary>
///     Struct containing a vulkan buffer with associated memory
/// </summary>
public sealed class MemoryBuffer : VulkanObject
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

    public readonly bool DisposeMemoryBlock;
    private readonly IMemoryManager MemoryManager;

    public MemoryBuffer(IVulkanEngine vulkanEngine, IAllocationHandler allocationHandler, IMemoryManager memoryManager,
        MemoryBlock memoryBlock, Buffer buffer, ulong size, bool disposeMemoryBlock = true) : base(vulkanEngine,
        allocationHandler)
    {
        Memory = memoryBlock;
        Buffer = buffer;
        Size = size;
        MemoryManager = memoryManager;
        DisposeMemoryBlock = disposeMemoryBlock;
    }

    protected override unsafe void ReleaseUnmanagedResources()
    {
        if (DisposeMemoryBlock)
        {
            MemoryManager.Free(Memory);
        }

        VulkanEngine.Vk.DestroyBuffer(VulkanEngine.Device, Buffer, null);
    }

    public unsafe Span<T> MapAs<T>() where T : unmanaged
    {
        if(Unsafe.SizeOf<T>() > (int)Size)
            throw new InvalidOperationException("Size of T is greater than the size of the buffer");
        
        var ptr = MemoryManager.Map(Memory);
        return new Span<T>(ptr.ToPointer(), (int)Size / Unsafe.SizeOf<T>());
    }
    
    public void Unmap()
    {
        MemoryManager.UnMap(Memory);
    }
}