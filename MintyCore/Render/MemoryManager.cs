using System;
using System.Collections.Generic;
using System.Diagnostics;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Render;

/// <summary>
/// Memory Manager class to handle native vulkan memory
/// </summary>
public static unsafe class MemoryManager
{
    private const ulong MinDedicatedAllocationSizeDynamic = 1024 * 1024 * 64;
    private const ulong MinDedicatedAllocationSizeNonDynamic = 1024 * 1024 * 256;

    private static readonly object _lock = new();

    private static readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryTypeUnmapped =
        new();

    private static readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryType =
        new();

    private static Device Device => VulkanEngine.Device;
    private static Vk Vk => VulkanEngine.Vk;

    /// <summary>
    /// Allocate a new Memory Block. Its recommended to directly use either <see cref="MemoryBuffer"/> or <see cref="Texture"/>
    /// </summary>
    /// <param name="memoryTypeBits">The memory Type bits <see cref="MemoryRequirements.MemoryTypeBits"/></param>
    /// <param name="flags">Memory property flags</param>
    /// <param name="persistentMapped">Should the memory block be persistently mapped</param>
    /// <param name="size">The size of the memory block</param>
    /// <param name="alignment">The alignment of the memory block</param>
    /// <param name="dedicated">Should a dedicated allocation be used</param>
    /// <param name="dedicatedImage"></param>
    /// <param name="dedicatedBuffer"></param>
    /// <returns>Allocated Memory Block</returns>
    public static MemoryBlock Allocate(
        uint memoryTypeBits,
        MemoryPropertyFlags flags,
        bool persistentMapped,
        ulong size,
        ulong alignment,
        bool dedicated = true,
        Image dedicatedImage = default,
        Buffer dedicatedBuffer = default)
    {
        // Round up to the nearest multiple of bufferImageGranularity.
        //size = (size / _bufferImageGranularity + 1) * _bufferImageGranularity;

        lock (_lock)
        {
            if (!VulkanUtils.FindMemoryType(memoryTypeBits, flags, out var memoryTypeIndex))
                Logger.WriteLog("No suitable memory type.", LogImportance.EXCEPTION, "Render");

            var minDedicatedAllocationSize = persistentMapped
                ? MinDedicatedAllocationSizeDynamic
                : MinDedicatedAllocationSizeNonDynamic;

            if (dedicated || size >= minDedicatedAllocationSize)
            {
                MemoryAllocateInfo allocateInfo = new()
                {
                    SType = StructureType.MemoryAllocateInfo,
                    AllocationSize = size,
                    MemoryTypeIndex = memoryTypeIndex
                };

                MemoryDedicatedAllocateInfoKHR dedicatedAI;
                if (dedicated)
                {
                    dedicatedAI = new MemoryDedicatedAllocateInfoKHR
                    {
                        SType = StructureType.MemoryDedicatedAllocateInfoKhr,
                        Buffer = dedicatedBuffer,
                        Image = dedicatedImage,
                    };
                    allocateInfo.PNext = &dedicatedAI;
                }

                var allocationResult = Vk.AllocateMemory(Device, allocateInfo, null, out var memory);
                if (allocationResult != Result.Success)
                    Logger.WriteLog("Unable to allocate sufficient Vulkan memory.", LogImportance.EXCEPTION,
                        "Render");

                void* mappedPtr = null;
                if (persistentMapped)
                {
                    var mapResult = Vk.MapMemory(Device, memory, 0, size, 0, &mappedPtr);
                    if (mapResult != Result.Success)
                        Logger.WriteLog("Unable to map newly-allocated Vulkan memory.", LogImportance.EXCEPTION,
                            "Render");
                }

                return new MemoryBlock(memory, 0, size, memoryTypeBits, mappedPtr, true);
            }

            var allocator = GetAllocator(memoryTypeIndex, persistentMapped);
            var result = allocator.Allocate(size, alignment, out var ret);
            if (!result)
                Logger.WriteLog("Unable to allocate sufficient Vulkan memory.", LogImportance.EXCEPTION,
                    "Render");

            return ret;
        }
    }

    /// <summary>
    /// Free a memory block
    /// </summary>
    /// <param name="block">To free</param>
    public static void Free(MemoryBlock block)
    {
        lock (_lock)
        {
            if (block.DedicatedAllocation)
                Vk.FreeMemory(Device, block.DeviceMemory, null);
            else
                GetAllocator(block.MemoryTypeIndex, block.IsPersistentMapped).InternalFree(block);
        }
    }

    private static ChunkAllocatorSet GetAllocator(uint memoryTypeIndex, bool persistentMapped)
    {
        ChunkAllocatorSet ret;
        if (persistentMapped)
        {
            if (!_allocatorsByMemoryType.TryGetValue(memoryTypeIndex, out ret))
            {
                ret = new ChunkAllocatorSet(Device, memoryTypeIndex, true);
                _allocatorsByMemoryType.Add(memoryTypeIndex, ret);
            }
        }
        else
        {
            if (!_allocatorsByMemoryTypeUnmapped.TryGetValue(memoryTypeIndex, out ret))
            {
                ret = new ChunkAllocatorSet(Device, memoryTypeIndex, false);
                _allocatorsByMemoryTypeUnmapped.Add(memoryTypeIndex, ret);
            }
        }

        return ret;
    }

    /// <summary>
    /// Clear the Memory Manager
    /// </summary>
    internal static void Clear()
    {
        // The clear method should only be called at the end of the application life cycle
        // ReSharper disable InconsistentlySynchronizedField
        foreach (var kvp in _allocatorsByMemoryType) kvp.Value.Dispose();
        foreach (var kvp in _allocatorsByMemoryTypeUnmapped) kvp.Value.Dispose();
        // ReSharper restore InconsistentlySynchronizedField
    }

    /// <summary>
    /// Map a memory block
    /// </summary>
    /// <param name="memoryBlock">to map</param>
    /// <returns><see cref="IntPtr"/> to the data</returns>
    public static IntPtr Map(MemoryBlock memoryBlock)
    {
        if (memoryBlock.IsPersistentMapped) return new IntPtr(memoryBlock.BaseMappedPointer);
        void* ret;
        VulkanUtils.Assert(Vk.MapMemory(Device, memoryBlock.DeviceMemory, memoryBlock.Offset, memoryBlock.Size, 0,
            &ret));
        return (IntPtr)ret;
    }

    /// <summary>
    /// Unmap a memory block
    /// </summary>
    /// <param name="memoryBlock">to unmap</param>
    public static void UnMap(MemoryBlock memoryBlock)
    {
        if (!memoryBlock.IsPersistentMapped)
            Vk.UnmapMemory(Device, memoryBlock.DeviceMemory);
    }

    private class ChunkAllocatorSet : IDisposable
    {
        private readonly List<ChunkAllocator> _allocators = new();
        private readonly Device _device;
        private readonly uint _memoryTypeIndex;
        private readonly bool _persistentMapped;

        public ChunkAllocatorSet(Device device, uint memoryTypeIndex, bool persistentMapped)
        {
            _device = device;
            _memoryTypeIndex = memoryTypeIndex;
            _persistentMapped = persistentMapped;
        }

        public void Dispose()
        {
            foreach (var allocator in _allocators) allocator.Dispose();
        }

        public bool Allocate(ulong size, ulong alignment, out MemoryBlock block)
        {
            foreach (var allocator in _allocators)
                if (allocator.Allocate(size, alignment, out block))
                    return true;

            var newAllocator = new ChunkAllocator(_device, _memoryTypeIndex, _persistentMapped);
            _allocators.Add(newAllocator);
            return newAllocator.Allocate(size, alignment, out block);
        }

        public void InternalFree(MemoryBlock block)
        {
            foreach (var chunk in _allocators)
                if (chunk.Memory.Handle == block.DeviceMemory.Handle)
                    chunk.InternalFree(block);
        }
    }

    private class ChunkAllocator : IDisposable
    {
        private const ulong PersistentMappedChunkSize = 1024 * 1024 * 64;
        private const ulong UnmappedChunkSize = 1024 * 1024 * 256;
        private readonly Device _device;
        private readonly List<MemoryBlock> _freeBlocks = new();
        private readonly void* _mappedPtr;
        private readonly DeviceMemory _memory;
        private readonly uint _memoryTypeIndex;

        public ChunkAllocator(Device device, uint memoryTypeIndex, bool persistentMapped)
        {
            _device = device;
            _memoryTypeIndex = memoryTypeIndex;
            var totalMemorySize = persistentMapped ? PersistentMappedChunkSize : UnmappedChunkSize;

            MemoryAllocateInfo memoryAi = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = totalMemorySize,
                MemoryTypeIndex = _memoryTypeIndex
            };

            VulkanUtils.Assert(Vk.AllocateMemory(_device, memoryAi, null, out _memory));

            void* mappedPtr = null;
            if (persistentMapped)
                VulkanUtils.Assert(Vk.MapMemory(_device, _memory, 0, totalMemorySize, 0, &mappedPtr));

            _mappedPtr = mappedPtr;

            var initialBlock = new MemoryBlock(
                _memory,
                0,
                totalMemorySize,
                _memoryTypeIndex,
                _mappedPtr,
                false);
            _freeBlocks.Add(initialBlock);
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        private static Vk Vk => VulkanEngine.Vk;

        public DeviceMemory Memory => _memory;

        public void Dispose()
        {
            Vk.FreeMemory(_device, _memory, null);
        }

        public bool Allocate(ulong size, ulong alignment, out MemoryBlock block)
        {
            checked
            {
                for (var i = 0; i < _freeBlocks.Count; i++)
                {
                    var freeBlock = _freeBlocks[i];
                    var alignedBlockSize = freeBlock.Size;
                    if (freeBlock.Offset % alignment != 0)
                    {
                        var alignmentCorrection = alignment - freeBlock.Offset % alignment;
                        if (alignedBlockSize <= alignmentCorrection) continue;

                        alignedBlockSize -= alignmentCorrection;
                    }

                    if (alignedBlockSize >= size) // Valid match -- split it and return.
                    {
                        _freeBlocks.RemoveAt(i);

                        freeBlock.Size = alignedBlockSize;
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if (freeBlock.Offset % alignment != 0)
                            freeBlock.Offset += alignment - freeBlock.Offset % alignment;

                        block = freeBlock;

                        if (alignedBlockSize != size)
                        {
                            var splitBlock = new MemoryBlock(
                                freeBlock.DeviceMemory,
                                freeBlock.Offset + size,
                                freeBlock.Size - size,
                                _memoryTypeIndex,
                                freeBlock.BaseMappedPointer,
                                false);
                            _freeBlocks.Insert(i, splitBlock);
                            block = freeBlock;
                            block.Size = size;
                        }

#if DEBUG
                        CheckAllocatedBlock(block);
#endif
                        return true;
                    }
                }

                block = default(MemoryBlock);
                return false;
            }
        }

        public void InternalFree(MemoryBlock block)
        {
            for (var i = 0; i < _freeBlocks.Count; i++)
                if (_freeBlocks[i].Offset > block.Offset)
                {
                    _freeBlocks.Insert(i, block);
                    MergeContiguousBlocks();
#if DEBUG
                    RemoveAllocatedBlock(block);
#endif
                    return;
                }

            _freeBlocks.Add(block);
#if DEBUG
            RemoveAllocatedBlock(block);
#endif
        }

        private void MergeContiguousBlocks()
        {
            var contiguousLength = 1;
            for (var i = 0; i < _freeBlocks.Count - 1; i++)
            {
                var blockStart = _freeBlocks[i].Offset;
                while (i + contiguousLength < _freeBlocks.Count
                       && _freeBlocks[i + contiguousLength - 1].End == _freeBlocks[i + contiguousLength].Offset)
                    contiguousLength += 1;

                if (contiguousLength > 1)
                {
                    var blockEnd = _freeBlocks[i + contiguousLength - 1].End;
                    _freeBlocks.RemoveRange(i, contiguousLength);
                    var mergedBlock = new MemoryBlock(
                        Memory,
                        blockStart,
                        blockEnd - blockStart,
                        _memoryTypeIndex,
                        _mappedPtr,
                        false);
                    _freeBlocks.Insert(i, mergedBlock);
                    contiguousLength = 0;
                }
            }
        }

#if DEBUG
        private List<MemoryBlock> _allocatedBlocks = new();

        private void CheckAllocatedBlock(MemoryBlock block)
        {
            foreach (var oldBlock in _allocatedBlocks)
                Debug.Assert(!BlocksOverlap(block, oldBlock), "Allocated blocks have overlapped.");

            _allocatedBlocks.Add(block);
        }

        private bool BlocksOverlap(MemoryBlock first, MemoryBlock second)
        {
            var firstStart = first.Offset;
            var firstEnd = first.Offset + first.Size;
            var secondStart = second.Offset;
            var secondEnd = second.Offset + second.Size;

            return firstStart <= secondStart && firstEnd > secondStart
                   || firstStart >= secondStart && firstEnd <= secondEnd
                   || firstStart < secondEnd && firstEnd >= secondEnd
                   || firstStart <= secondStart && firstEnd >= secondEnd;
        }

        private void RemoveAllocatedBlock(MemoryBlock block)
        {
            Debug.Assert(_allocatedBlocks.Remove(block), "Unable to remove a supposedly allocated block.");
        }
#endif
    }
}

/// <summary>
/// Struct which contains native vulkan device memory
/// </summary>
[DebuggerDisplay("[Mem:{DeviceMemory.Handle}] Off:{Offset}, Size:{Size} End:{Offset+Size}")]
public unsafe struct MemoryBlock : IEquatable<MemoryBlock>
{
    ///<summary/>
    public readonly uint MemoryTypeIndex;

    /// <summary>
    /// The device memory of this block
    /// </summary>
    public readonly DeviceMemory DeviceMemory;

    ///<summary/>
    public readonly void* BaseMappedPointer;

    ///<summary/>
    public readonly bool DedicatedAllocation;

    ///<summary/>
    public ulong Offset;

    ///<summary/>
    public ulong Size;

    ///<summary/>
    public void* BlockMappedPointer => (byte*)BaseMappedPointer + Offset;

    ///<summary/>
    public bool IsPersistentMapped => BaseMappedPointer != null;

    ///<summary/>
    public ulong End => Offset + Size;

    ///<summary/>
    public MemoryBlock(
        DeviceMemory memory,
        ulong offset,
        ulong size,
        uint memoryTypeIndex,
        void* mappedPtr,
        bool dedicatedAllocation)
    {
        DeviceMemory = memory;
        Offset = offset;
        Size = size;
        MemoryTypeIndex = memoryTypeIndex;
        BaseMappedPointer = mappedPtr;
        DedicatedAllocation = dedicatedAllocation;
    }

    ///<summary/>
    public bool Equals(MemoryBlock other)
    {
        return DeviceMemory.Equals(other.DeviceMemory)
               && Offset.Equals(other.Offset)
               && Size.Equals(other.Size);
    }

    ///<summary/>
    public override bool Equals(object obj)
    {
        return obj is MemoryBlock block && Equals(block);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return DeviceMemory.Handle.GetHashCode();
    }
}