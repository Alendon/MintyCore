using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

/// <summary>
///     Class to handle the creation and destruction of <see cref="DescriptorSet" />
/// </summary>
public static unsafe class DescriptorSetHandler
{
    private const uint PoolCapacity = 100;
    private static readonly Dictionary<DescriptorType, uint> _poolSizes = new();
    private static readonly Dictionary<Identification, DescriptorSetLayout> _descriptorSetLayouts = new();
    private static readonly Dictionary<DescriptorPool, HashSet<DescriptorSet>> _allocatedDescriptorSets = new();
    private static readonly Dictionary<Identification, HashSet<DescriptorSet>> _descriptorSetTrack = new();
    private static readonly Dictionary<DescriptorSet, Identification> _reversedDescriptorSetTrack = new();

    /// <summary>
    ///     Free a previously allocated descriptor set
    /// </summary>
    /// <param name="set">to free</param>
    public static void FreeDescriptorSet(DescriptorSet set)
    {
        foreach (var (pool, sets) in _allocatedDescriptorSets)
        {
            if (!sets.Contains(set)) continue;
            VulkanUtils.Assert(VulkanEngine.Vk.FreeDescriptorSets(VulkanEngine.Device, pool, 1, set));
            UnTrackDescriptorSet(set);
            break;
        }
    }

    /// <summary>
    ///     Allocate a new descriptor set, based on the layout id
    /// </summary>
    /// <param name="descriptorSetLayoutId"></param>
    /// <param name="count">Optional count if you're using a variable descriptor count</param>
    /// <returns>New allocated descriptor set</returns>
    public static DescriptorSet AllocateDescriptorSet(Identification descriptorSetLayoutId, int count = 0)
    {
        //Get a pool for the descriptor to allocate from
        DescriptorPool pool = default;
        foreach (var (descPool, descriptors) in _allocatedDescriptorSets)
        {
            if (descriptors.Count >= PoolCapacity) continue;
            pool = descPool;
            break;
        }

        if (pool.Handle == default) pool = CreateDescriptorPool();

        DescriptorSetVariableDescriptorCountAllocateInfo countInfo = new()
        {
            SType = StructureType.DescriptorSetVariableDescriptorCountAllocateInfo,
            DescriptorSetCount = 1,
            PDescriptorCounts = (uint*) &count
        };

        //Allocate the descriptor set
        var layout = _descriptorSetLayouts[descriptorSetLayoutId];
        DescriptorSetAllocateInfo allocateInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            PNext = count == 0 ? null : &countInfo,
            DescriptorPool = pool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout
        };
        VulkanUtils.Assert(VulkanEngine.Vk.AllocateDescriptorSets(VulkanEngine.Device, allocateInfo, out var set));
        _allocatedDescriptorSets[pool].Add(set);
        TrackDescriptorSet(descriptorSetLayoutId, set);
        return set;
    }

    internal static void AddDescriptorSetLayout(Identification layoutId,
        ReadOnlySpan<DescriptorSetLayoutBinding> bindings,
        DescriptorBindingFlags[]? descriptorBindingFlagsArray,
        DescriptorSetLayoutCreateFlags createFlags)
    {
        DescriptorSetLayout layout;

        Span<DescriptorBindingFlags> descriptorBindingFlags =
            descriptorBindingFlagsArray ?? Array.Empty<DescriptorBindingFlags>();

        fixed (DescriptorSetLayoutBinding* pBinding = &bindings.GetPinnableReference())
        fixed (DescriptorBindingFlags* pFlags = &descriptorBindingFlags.GetPinnableReference())
        {
            DescriptorSetLayoutBindingFlagsCreateInfoEXT bindingFlagsCreateInfo = new()
            {
                SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfoExt,
                BindingCount = (uint) descriptorBindingFlags.Length,
                PBindingFlags = pFlags
            };

            DescriptorSetLayoutCreateInfo createInfo = new()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PNext = descriptorBindingFlagsArray is not null ? &bindingFlagsCreateInfo : null,
                Flags = createFlags,
                BindingCount = (uint) bindings.Length,
                PBindings = pBinding
            };

            VulkanUtils.Assert(VulkanEngine.Vk.CreateDescriptorSetLayout(VulkanEngine.Device, createInfo,
                VulkanEngine.AllocationCallback, out layout));
        }


        foreach (var binding in bindings)
        {
            if (!_poolSizes.ContainsKey(binding.DescriptorType)) _poolSizes.Add(binding.DescriptorType, 0);
            if (_poolSizes[binding.DescriptorType] < binding.DescriptorCount)
                _poolSizes[binding.DescriptorType] = binding.DescriptorCount;
        }

        _descriptorSetLayouts.Add(layoutId, layout);
        _descriptorSetTrack.Add(layoutId, new HashSet<DescriptorSet>());
    }

    private static DescriptorPool CreateDescriptorPool()
    {
        var poolSizeCount = _poolSizes.Count;
        Span<DescriptorPoolSize> poolSizes = stackalloc DescriptorPoolSize[poolSizeCount];
        var iteration = 0;
        foreach (var (descriptorType, _) in _poolSizes)
        {
            poolSizes[iteration] = new DescriptorPoolSize
            {
                Type = descriptorType, DescriptorCount = PoolCapacity
            };
            iteration++;
        }

        DescriptorPoolCreateInfo createInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PNext = null,
            PPoolSizes = (DescriptorPoolSize*) Unsafe.AsPointer(ref poolSizes.GetPinnableReference()),
            MaxSets = PoolCapacity,
            Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit |
                    DescriptorPoolCreateFlags.UpdateAfterBindBit,
            PoolSizeCount = (uint) poolSizeCount
        };

        VulkanUtils.Assert(VulkanEngine.Vk.CreateDescriptorPool(VulkanEngine.Device, createInfo,
            VulkanEngine.AllocationCallback, out var pool));
        _allocatedDescriptorSets.Add(pool, new HashSet<DescriptorSet>());
        return pool;
    }

    internal static void Clear()
    {
        foreach (var pool in _allocatedDescriptorSets.Keys)
            VulkanEngine.Vk.DestroyDescriptorPool(VulkanEngine.Device, pool, VulkanEngine.AllocationCallback);

        foreach (var layout in _descriptorSetLayouts.Values)
            VulkanEngine.Vk.DestroyDescriptorSetLayout(VulkanEngine.Device, layout,
                VulkanEngine.AllocationCallback);

        _poolSizes.Clear();
        _allocatedDescriptorSets.Clear();
        _descriptorSetLayouts.Clear();
    }

    /// <summary>
    ///     Get a specific descriptor set layout
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static DescriptorSetLayout GetDescriptorSetLayout(Identification id)
    {
        return _descriptorSetLayouts[id];
    }

    private static void TrackDescriptorSet(Identification descriptorTypeId, DescriptorSet descriptorSet)
    {
        if (!Engine.TestingModeActive) return;
        _reversedDescriptorSetTrack.Add(descriptorSet, descriptorTypeId);
        _descriptorSetTrack[descriptorTypeId].Add(descriptorSet);
    }

    private static void UnTrackDescriptorSet(DescriptorSet descriptorSet)
    {
        if (!Engine.TestingModeActive) return;
        if (_reversedDescriptorSetTrack.Remove(descriptorSet, out var id))
            _descriptorSetTrack[id].Remove(descriptorSet);
    }

    private static bool TrackedDescriptorTypeEmpty(Identification descriptorTypeId)
    {
        if (!Engine.TestingModeActive) return true;
        return _descriptorSetTrack[descriptorTypeId].Count == 0;
    }

    internal static void RemoveDescriptorSetLayout(Identification objectId)
    {
        if (!TrackedDescriptorTypeEmpty(objectId))
        {
            Logger.WriteLog(
                $"Cant remove descriptor set layout {objectId}; Not all allocated descriptor sets have been freed",
                LogImportance.Error, "Render");
            return;
        }

        if (_descriptorSetLayouts.Remove(objectId, out var layout))
            VulkanEngine.Vk.DestroyDescriptorSetLayout(VulkanEngine.Device, layout,
                VulkanEngine.AllocationCallback);
    }

    private class ManagedDescriptorPool
    {
        private readonly DescriptorPoolSize[] _descriptorPoolSizes;
        private readonly DescriptorSetLayout _descriptorSetLayout;
        private uint _maxSetCount = 100;

        private Dictionary<DescriptorSet, uint> _descriptorSetPoolIds = new();
        private Dictionary<uint, DescriptorPool> _descriptorPools = new();
        private Dictionary<uint, uint> _descriptorPoolUsage = new();

        public ManagedDescriptorPool(ReadOnlySpan<DescriptorSetLayoutBinding> bindings,
            DescriptorBindingFlags[]? descriptorBindingFlagsArray,
            DescriptorSetLayoutCreateFlags createFlags, uint setsPerPool)
        {
            _maxSetCount = setsPerPool;

            if (descriptorBindingFlagsArray is not null)
            {
                Logger.AssertAndThrow(descriptorBindingFlagsArray.Length == bindings.Length,
                    "Descriptor binding flags array length must match the number of bindings", "DescriptorSetHandler");

                Logger.AssertAndThrow(
                    !descriptorBindingFlagsArray.Any(x => x.HasFlag(DescriptorBindingFlags.VariableDescriptorCountBit)),
                    "Variable descriptor count is only supported through the variable descriptor registry option",
                    "DescriptorSetHandler");
            }
            
            CalculateDescriptorPoolSize(bindings, _maxSetCount, out _descriptorPoolSizes);

            _descriptorSetLayout = CreateDescriptorSetLayout(bindings, descriptorBindingFlagsArray, createFlags);
        }

        private static void CalculateDescriptorPoolSize(ReadOnlySpan<DescriptorSetLayoutBinding> bindings,
            uint maxSetCount, out DescriptorPoolSize[] poolSizes)
        {
            Dictionary<DescriptorType, uint> descriptorTypeCounts = new();
            foreach (var binding in bindings)
            {
                descriptorTypeCounts.TryGetValue(binding.DescriptorType, out var count);
                descriptorTypeCounts[binding.DescriptorType] = count + binding.DescriptorCount;
            }
            
            poolSizes = new DescriptorPoolSize[descriptorTypeCounts.Count];
            var iteration = 0;
            foreach (var (descriptorType, count) in descriptorTypeCounts)
            {
                poolSizes[iteration++] = new DescriptorPoolSize
                {
                    Type = descriptorType,
                    DescriptorCount = count * maxSetCount
                };
            }
        }

        private static DescriptorSetLayout CreateDescriptorSetLayout(ReadOnlySpan<DescriptorSetLayoutBinding> bindings,
            DescriptorBindingFlags[]? descriptorBindingFlagsArray, DescriptorSetLayoutCreateFlags createFlags)
        {
            
        }

        public DescriptorSet AllocateDescriptorSet()
        {
            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
            }
        }

        private uint GetAvailableDescriptorPoolId(uint availableSets)
        {
            var searchResults = _descriptorPoolUsage.Where(
                entry => _maxSetCount - entry.Value >= availableSets).ToArray();

            return searchResults.Length != 0 ? searchResults[0].Key : CreateDescriptorPool();
        }

        private uint CreateDescriptorPool()
        {
            var poolSizes = _descriptorPoolSizes.AsSpan();
            var poolSizeHandle = GCHandle.Alloc(_descriptorPoolSizes, GCHandleType.Pinned);


            DescriptorPoolCreateInfo createInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint) poolSizes.Length,
                PPoolSizes = (DescriptorPoolSize*) Unsafe.AsPointer(ref poolSizes.GetPinnableReference()),
                MaxSets = _maxSetCount,
                Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit
            };

            VulkanUtils.Assert(VulkanEngine.Vk.CreateDescriptorPool(
                VulkanEngine.Device, createInfo, VulkanEngine.AllocationCallback, out var pool));

            poolSizeHandle.Free();

            var poolId = (uint) _descriptorPools.Count;
            _descriptorPools.Add(poolId, pool);
            _descriptorPoolUsage.Add(poolId, (_maxSetCount, 0));

            return poolId;
        }
    }
}