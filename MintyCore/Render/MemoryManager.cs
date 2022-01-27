using System;
using System.Collections.Generic;
using System.Diagnostics;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Render;

public static unsafe class MemoryManager
{
    private const ulong MinDedicatedAllocationSizeDynamic = 1024 * 1024 * 64;
    private const ulong MinDedicatedAllocationSizeNonDynamic = 1024 * 1024 * 256;

    private static bool _granularitySet;
    private static ulong _granularityValue;

    private static readonly object _lock = new();
    private static ulong _totalAllocatedBytes;

    private static readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryTypeUnmapped =
        new();

    private static readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryType =
        new();

    private static Device _device => VulkanEngine.Device;
    private static PhysicalDevice _physicalDevice => VulkanEngine.PhysicalDevice;
    private static Vk Vk => VulkanEngine.Vk;

    private static ulong _bufferImageGranularity
    {
        get
        {
            if (_granularitySet) return _granularityValue;
            VulkanEngine.Vk.GetPhysicalDeviceProperties(_physicalDevice, out var props);
            _granularityValue = props.Limits.BufferImageGranularity;
            _granularitySet = true;
            return _granularityValue;
        }
    }


    public static MemoryBlock Allocate(
        uint memoryTypeBits,
        MemoryPropertyFlags flags,
        bool persistentMapped,
        ulong size,
        ulong alignment,
        bool dedicated = false,
        Image dedicatedImage = default,
        Buffer dedicatedBuffer = default)
    {
        // Round up to the nearest multiple of bufferImageGranularity.
        //size = (size / _bufferImageGranularity + 1) * _bufferImageGranularity;
        _totalAllocatedBytes += size;

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

                var allocationResult = Vk.AllocateMemory(_device, allocateInfo, null, out var memory);
                if (allocationResult != Result.Success)
                    Logger.WriteLog("Unable to allocate sufficient Vulkan memory.", LogImportance.EXCEPTION,
                        "Render");

                void* mappedPtr = null;
                if (persistentMapped)
                {
                    var mapResult = Vk.MapMemory(_device, memory, 0, size, 0, &mappedPtr);
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

    public static void Free(MemoryBlock block)
    {
        _totalAllocatedBytes -= block.Size;
        lock (_lock)
        {
            if (block.DedicatedAllocation)
                Vk.FreeMemory(_device, block.DeviceMemory, null);
            else
                GetAllocator(block.MemoryTypeIndex, block.IsPersistentMapped).Free(block);
        }
    }

    private static ChunkAllocatorSet GetAllocator(uint memoryTypeIndex, bool persistentMapped)
    {
        ChunkAllocatorSet ret = null;
        if (persistentMapped)
        {
            if (!_allocatorsByMemoryType.TryGetValue(memoryTypeIndex, out ret))
            {
                ret = new ChunkAllocatorSet(_device, memoryTypeIndex, true);
                _allocatorsByMemoryType.Add(memoryTypeIndex, ret);
            }
        }
        else
        {
            if (!_allocatorsByMemoryTypeUnmapped.TryGetValue(memoryTypeIndex, out ret))
            {
                ret = new ChunkAllocatorSet(_device, memoryTypeIndex, false);
                _allocatorsByMemoryTypeUnmapped.Add(memoryTypeIndex, ret);
            }
        }

        return ret;
    }

    public static void Clear()
    {
        foreach (var kvp in _allocatorsByMemoryType) kvp.Value.Dispose();

        foreach (var kvp in _allocatorsByMemoryTypeUnmapped) kvp.Value.Dispose();
    }

    internal static IntPtr Map(MemoryBlock memoryBlock)
    {
        if (memoryBlock.IsPersistentMapped) return new IntPtr(memoryBlock.BaseMappedPointer);
        void* ret;
        VulkanUtils.Assert(Vk.MapMemory(_device, memoryBlock.DeviceMemory, memoryBlock.Offset, memoryBlock.Size, 0,
            &ret));
        return (IntPtr)ret;
    }

    public static void UnMap(MemoryBlock memoryBlock)
    {
        if (!memoryBlock.IsPersistentMapped)
            Vk.UnmapMemory(_device, memoryBlock.DeviceMemory);
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

        public void Free(MemoryBlock block)
        {
            foreach (var chunk in _allocators)
                if (chunk.Memory.Handle == block.DeviceMemory.Handle)
                    chunk.Free(block);
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
        private readonly bool _persistentMapped;
        private ulong _totalAllocatedBytes;

        private ulong _totalMemorySize;

        public ChunkAllocator(Device device, uint memoryTypeIndex, bool persistentMapped)
        {
            _device = device;
            _memoryTypeIndex = memoryTypeIndex;
            _persistentMapped = persistentMapped;
            _totalMemorySize = persistentMapped ? PersistentMappedChunkSize : UnmappedChunkSize;

            MemoryAllocateInfo memoryAI = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = _totalMemorySize,
                MemoryTypeIndex = _memoryTypeIndex
            };

            VulkanUtils.Assert(Vk.AllocateMemory(_device, memoryAI, null, out _memory));

            void* mappedPtr = null;
            if (persistentMapped)
                VulkanUtils.Assert(Vk.MapMemory(_device, _memory, 0, _totalMemorySize, 0, &mappedPtr));

            _mappedPtr = mappedPtr;

            var initialBlock = new MemoryBlock(
                _memory,
                0,
                _totalMemorySize,
                _memoryTypeIndex,
                _mappedPtr,
                false);
            _freeBlocks.Add(initialBlock);
        }

        private Vk Vk => VulkanEngine.Vk;

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
                        _totalAllocatedBytes += alignedBlockSize;
                        return true;
                    }
                }

                block = default(MemoryBlock);
                return false;
            }
        }

        public void Free(MemoryBlock block)
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
            _totalAllocatedBytes -= block.Size;
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

[DebuggerDisplay("[Mem:{DeviceMemory.Handle}] Off:{Offset}, Size:{Size} End:{Offset+Size}")]
public unsafe struct MemoryBlock : IEquatable<MemoryBlock>
{
    public readonly uint MemoryTypeIndex;
    public readonly DeviceMemory DeviceMemory;
    public void* BaseMappedPointer;
    public readonly bool DedicatedAllocation;

    public ulong Offset;
    public ulong Size;

    public void* BlockMappedPointer => (byte*)BaseMappedPointer + Offset;
    public bool IsPersistentMapped => BaseMappedPointer != null;
    public ulong End => Offset + Size;

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

    public bool Equals(MemoryBlock other)
    {
        return DeviceMemory.Equals(other.DeviceMemory)
               && Offset.Equals(other.Offset)
               && Size.Equals(other.Size);
    }
}