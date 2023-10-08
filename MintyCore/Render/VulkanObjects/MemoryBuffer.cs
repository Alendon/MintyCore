using MintyCore.Render.Managers;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Utils;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Render.VulkanObjects;

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

    public MemoryBuffer(IVulkanEngine vulkanEngine, IAllocationTracker allocationTracker, IMemoryManager memoryManager,
        MemoryBlock memoryBlock, Buffer buffer, ulong size, bool disposeMemoryBlock = true) : base(vulkanEngine,
        allocationTracker)
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
}