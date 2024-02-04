using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MintyCore.Graphics.Utils;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Graphics.Managers.Implementations;

/// <summary>
///     Memory Manager class to handle native vulkan memory
/// </summary>
[Singleton<IMemoryManager>(SingletonContextFlags.NoHeadless)]
internal unsafe class MemoryManager : IMemoryManager
{
    private const ulong MinDedicatedAllocationSizeDynamic = 1024 * 1024 * 64;
    private const ulong MinDedicatedAllocationSizeNonDynamic = 1024 * 1024 * 256;

    private readonly object _lock = new();

    private readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryTypeUnmapped =
        new();

    private readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryTypeAddressable =
        new();

    private readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryType =
        new();

    public required IVulkanEngine VulkanEngine { private get; init; }
    public required IAllocationHandler AllocationHandler { private get; init; }

    private Device Device => VulkanEngine.Device;
    private Vk Vk => VulkanEngine.Vk;

    public MemoryBuffer CreateBuffer(BufferUsageFlags bufferUsage, ulong size,
        Span<uint> queueFamilyIndices, MemoryPropertyFlags memoryPropertyFlags, bool stagingBuffer,
        SharingMode sharingMode = SharingMode.Exclusive,
        bool dedicated = false, BufferCreateFlags bufferCreateFlags = 0)
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
                QueueFamilyIndexCount = (uint)queueFamilyIndices.Length,
                PQueueFamilyIndices = queueFamilyIndex
            };
            VulkanUtils.Assert(VulkanEngine.Vk.CreateBuffer(VulkanEngine.Device, createInfo, null, out buffer));
        }

        bool addressable = (BufferUsageFlags.ShaderDeviceAddressBit & bufferUsage) != 0;


        VulkanEngine.Vk.GetBufferMemoryRequirements(VulkanEngine.Device, buffer, out var memoryRequirements);
        var memory = Allocate(memoryRequirements.MemoryTypeBits, memoryPropertyFlags, stagingBuffer,
            memoryRequirements.Size, memoryRequirements.Alignment, dedicated, default, buffer, addressable);


        VulkanUtils.Assert(VulkanEngine.Vk.BindBufferMemory(VulkanEngine.Device, buffer, memory.DeviceMemory, memory.Offset));

        return new MemoryBuffer(VulkanEngine, AllocationHandler, this, memory, buffer, size);
    }

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
    public MemoryBlock Allocate(
        uint memoryTypeBits,
        MemoryPropertyFlags flags,
        bool persistentMapped,
        ulong size,
        ulong alignment,
        bool dedicated = true,
        Image dedicatedImage = default,
        Buffer dedicatedBuffer = default,
        bool addressable = false)
    {
        // Round up to the nearest multiple of bufferImageGranularity.
        //size = (size / _bufferImageGranularity + 1) * _bufferImageGranularity;

        lock (_lock)
        {
            if (!VulkanEngine.FindMemoryType(memoryTypeBits, flags, out var memoryTypeIndex))
                Logger.WriteLog("No suitable memory type.", LogImportance.Exception, "Render");

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

                if (addressable)
                {
                    MemoryAllocateFlagsInfo flagsInfo = new()
                    {
                        SType = StructureType.MemoryAllocateFlagsInfo,
                        Flags = MemoryAllocateFlags.AddressBit | MemoryAllocateFlags.AddressCaptureReplayBit,
                        PNext = allocateInfo.PNext
                    };
                    allocateInfo.PNext = &flagsInfo;
                }

                if (dedicated)
                {
                    var dedicatedAi = new MemoryDedicatedAllocateInfoKHR
                    {
                        SType = StructureType.MemoryDedicatedAllocateInfoKhr,
                        Buffer = dedicatedBuffer,
                        Image = dedicatedImage,
                        PNext = allocateInfo.PNext
                    };
                    allocateInfo.PNext = &dedicatedAi;
                }

                var allocationResult = Vk.AllocateMemory(Device, allocateInfo, null, out var memory);
                if (allocationResult != Result.Success)
                    Logger.WriteLog("Unable to allocate sufficient Vulkan memory.", LogImportance.Exception,
                        "Render");

                void* mappedPtr = null;
                if (!persistentMapped)
                    return new MemoryBlock(memory, 0, size, memoryTypeBits, mappedPtr, true, addressable);
                var mapResult = Vk.MapMemory(Device, memory, 0, size, 0, &mappedPtr);
                if (mapResult != Result.Success)
                    Logger.WriteLog("Unable to map newly-allocated Vulkan memory.", LogImportance.Exception,
                        "Render");

                return new MemoryBlock(memory, 0, size, memoryTypeBits, mappedPtr, true, addressable);
            }

            var allocator = GetAllocator(memoryTypeIndex, persistentMapped, addressable);
            var result = allocator.Allocate(size, alignment, out var ret);
            if (!result)
                Logger.WriteLog("Unable to allocate sufficient Vulkan memory.", LogImportance.Exception,
                    "Render");

            return ret;
        }
    }

    /// <summary>
    ///     Free a memory block
    /// </summary>
    /// <param name="block">To free</param>
    public void Free(MemoryBlock block)
    {
        lock (_lock)
        {
            if (block.DedicatedAllocation)
                Vk.FreeMemory(Device, block.DeviceMemory, null);
            else
                GetAllocator(block.MemoryTypeIndex, block.IsPersistentMapped, block.IsAddressable).InternalFree(block);
        }
    }

    private ChunkAllocatorSet GetAllocator(uint memoryTypeIndex, bool persistentMapped, bool addressable)
    {
        ChunkAllocatorSet? ret;
        Logger.AssertAndThrow(!(persistentMapped && addressable),
            "Cannot have persistent mapped memory cannot be addressable.", "MemoryManager");

        if (persistentMapped)
        {
            if (_allocatorsByMemoryType.TryGetValue(memoryTypeIndex, out ret)) return ret;
            ret = new ChunkAllocatorSet(Device, memoryTypeIndex, true, false, VulkanEngine);
            _allocatorsByMemoryType.Add(memoryTypeIndex, ret);
        }
        else if (addressable)
        {
            if (_allocatorsByMemoryTypeAddressable.TryGetValue(memoryTypeIndex, out ret)) return ret;
            ret = new ChunkAllocatorSet(Device, memoryTypeIndex, false, true, VulkanEngine);
            _allocatorsByMemoryTypeAddressable.Add(memoryTypeIndex, ret);
        }
        else
        {
            if (_allocatorsByMemoryTypeUnmapped.TryGetValue(memoryTypeIndex, out ret)) return ret;
            ret = new ChunkAllocatorSet(Device, memoryTypeIndex, false, false, VulkanEngine);
            _allocatorsByMemoryTypeUnmapped.Add(memoryTypeIndex, ret);
        }

        return ret;
    }

    /// <summary>
    ///     Clear the Memory Manager
    /// </summary>
    public void Clear()
    {
        // The clear method should only be called at the end of the application life cycle
        // ReSharper disable InconsistentlySynchronizedField
        foreach (var kvp in _allocatorsByMemoryType) kvp.Value.Dispose();
        foreach (var kvp in _allocatorsByMemoryTypeUnmapped) kvp.Value.Dispose();
        // ReSharper restore InconsistentlySynchronizedField
    }

    /// <summary>
    ///     Map a memory block
    /// </summary>
    /// <param name="memoryBlock">to map</param>
    /// <returns><see cref="IntPtr" /> to the data</returns>
    public IntPtr Map(MemoryBlock memoryBlock)
    {
        if (memoryBlock.IsPersistentMapped)
            return new IntPtr((long)memoryBlock.BaseMappedPointer + (long)memoryBlock.Offset);
        void* ret;
        VulkanUtils.Assert(Vk.MapMemory(Device, memoryBlock.DeviceMemory, memoryBlock.Offset, memoryBlock.Size, 0,
            &ret));
        return (IntPtr)ret;
    }

    /// <summary>
    ///     Unmap a memory block
    /// </summary>
    /// <param name="memoryBlock">to unmap</param>
    public void UnMap(MemoryBlock memoryBlock)
    {
        if (!memoryBlock.IsPersistentMapped)
            Vk.UnmapMemory(Device, memoryBlock.DeviceMemory);
    }

    private sealed class ChunkAllocatorSet : IDisposable
    {
        private readonly List<ChunkAllocator> _allocators = new();
        private readonly Device _device;
        private readonly uint _memoryTypeIndex;
        private readonly bool _persistentMapped;
        private readonly bool _addressable;

        private IVulkanEngine VulkanEngine { get; }

        public ChunkAllocatorSet(Device device, uint memoryTypeIndex, bool persistentMapped, bool addressable,
            IVulkanEngine vulkanEngine)
        {
            _device = device;
            _memoryTypeIndex = memoryTypeIndex;
            _persistentMapped = persistentMapped;
            _addressable = addressable;
            VulkanEngine = vulkanEngine;
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

            var newAllocator =
                new ChunkAllocator(_device, _memoryTypeIndex, _persistentMapped, _addressable, VulkanEngine);
            _allocators.Add(newAllocator);
            return newAllocator.Allocate(size, alignment, out block);
        }

        public void InternalFree(MemoryBlock block)
        {
            foreach (var chunk in _allocators.Where(chunk => chunk.Memory.Handle == block.DeviceMemory.Handle))
                chunk.InternalFree(block);
        }
    }

    private sealed class ChunkAllocator : IDisposable
    {
        private const ulong PersistentMappedChunkSize = 1024 * 1024 * 64;
        private const ulong UnmappedChunkSize = 1024 * 1024 * 256;
        private readonly Device _device;
        private readonly List<MemoryBlock> _freeBlocks = new();
        private readonly void* _mappedPtr;
        private readonly DeviceMemory _memory;
        private readonly uint _memoryTypeIndex;
        private readonly bool _isAddressable;

        public ChunkAllocator(Device device, uint memoryTypeIndex, bool persistentMapped, bool addressable,
            IVulkanEngine vulkanEngine)
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

            _isAddressable = addressable;
            VulkanEngine = vulkanEngine;
            if (addressable)
            {
                MemoryAllocateFlagsInfo flagsInfo = new()
                {
                    SType = StructureType.MemoryAllocateFlagsInfo,
                    Flags = MemoryAllocateFlags.AddressBit | MemoryAllocateFlags.AddressCaptureReplayBit,
                    PNext = memoryAi.PNext
                };
                memoryAi.PNext = &flagsInfo;
            }

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
                false,
                addressable);
            _freeBlocks.Add(initialBlock);
        }

        private IVulkanEngine VulkanEngine { get; }

        // ReSharper disable once MemberHidesStaticFromOuterClass
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

                    if (alignedBlockSize < size) continue;
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
                            false,
                            _isAddressable);
                        _freeBlocks.Insert(i, splitBlock);
                        block = freeBlock;
                        block.Size = size;
                    }

                    if (Engine.TestingModeActive)
                        CheckAllocatedBlock(block);

                    return true;
                }

                block = default;
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
                    if (Engine.TestingModeActive)
                        RemoveAllocatedBlock(block);

                    return;
                }

            _freeBlocks.Add(block);
            if (Engine.TestingModeActive)
                RemoveAllocatedBlock(block);
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

                if (contiguousLength <= 1) continue;
                var blockEnd = _freeBlocks[i + contiguousLength - 1].End;
                _freeBlocks.RemoveRange(i, contiguousLength);
                var mergedBlock = new MemoryBlock(
                    Memory,
                    blockStart,
                    blockEnd - blockStart,
                    _memoryTypeIndex,
                    _mappedPtr,
                    false,
                    _isAddressable);
                _freeBlocks.Insert(i, mergedBlock);
                contiguousLength = 0;
            }
        }

        private readonly List<MemoryBlock> _allocatedBlocks = new();

        private void CheckAllocatedBlock(MemoryBlock block)
        {
            if (!Engine.TestingModeActive) return;
            foreach (var oldBlock in _allocatedBlocks)
                Debug.Assert(!BlocksOverlap(block, oldBlock), "Allocated blocks have overlapped.");

            _allocatedBlocks.Add(block);
        }

        private static bool BlocksOverlap(MemoryBlock first, MemoryBlock second)
        {
            var firstStart = first.Offset;
            var firstEnd = first.Offset + first.Size;
            var secondStart = second.Offset;
            var secondEnd = second.Offset + second.Size;

            return (firstStart <= secondStart && firstEnd > secondStart)
                   || (firstStart >= secondStart && firstEnd <= secondEnd)
                   || (firstStart < secondEnd && firstEnd >= secondEnd)
                   || (firstStart <= secondStart && firstEnd >= secondEnd);
        }

        private void RemoveAllocatedBlock(MemoryBlock block)
        {
            Debug.Assert(_allocatedBlocks.Remove(block), "Unable to remove a supposedly allocated block.");
        }
    }
}

/// <summary>
///     Struct which contains native vulkan device memory
/// </summary>
[DebuggerDisplay("[Mem:{DeviceMemory.Handle}] Off:{Offset}, Size:{Size} End:{Offset+Size}")]
public unsafe struct MemoryBlock : IEquatable<MemoryBlock>
{
    /// <summary />
    public readonly uint MemoryTypeIndex;

    /// <summary>
    ///     The device memory of this block
    /// </summary>
    public readonly DeviceMemory DeviceMemory;

    /// <summary />
    public readonly void* BaseMappedPointer;

    /// <summary />
    public readonly bool DedicatedAllocation;

    /// <summary />
    public ulong Offset;

    /// <summary />
    public ulong Size;

    /// <summary />
    public void* BlockMappedPointer => (byte*)BaseMappedPointer + Offset;

    /// <summary />
    public bool IsPersistentMapped => BaseMappedPointer != null;

    public bool IsAddressable { get; init; }

    /// <summary />
    public ulong End => Offset + Size;

    /// <summary />
    public MemoryBlock(
        DeviceMemory memory,
        ulong offset,
        ulong size,
        uint memoryTypeIndex,
        void* mappedPtr,
        bool dedicatedAllocation,
        bool addressable)
    {
        DeviceMemory = memory;
        Offset = offset;
        Size = size;
        MemoryTypeIndex = memoryTypeIndex;
        BaseMappedPointer = mappedPtr;
        DedicatedAllocation = dedicatedAllocation;
        IsAddressable = addressable;
    }

    /// <summary />
    public bool Equals(MemoryBlock other)
    {
        return DeviceMemory.Equals(other.DeviceMemory)
               && Offset.Equals(other.Offset)
               && Size.Equals(other.Size);
    }

    /// <summary />
    public override bool Equals(object? obj)
    {
        return obj is MemoryBlock block && Equals(block);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return DeviceMemory.Handle.GetHashCode();
    }
}