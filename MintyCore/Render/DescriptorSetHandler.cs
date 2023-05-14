using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

/// <summary>
///     Class to handle the creation and destruction of <see cref="DescriptorSet" />
/// </summary>
public static class DescriptorSetHandler
{
    private static readonly Dictionary<Identification, DescriptorTrackingType> _descriptorSetTypes = new();
    private static readonly Dictionary<Identification, ManagedDescriptorPool> _managedDescriptorPools = new();
    private static readonly Dictionary<DescriptorSet, Identification> _descriptorSetIdTrack = new();

    private static readonly Dictionary<Identification, DescriptorSetLayout> _externalDescriptorSetLayouts = new();

    private enum DescriptorTrackingType
    {
        Normal,
        Variable,
        External
    }

    /// <summary>
    ///     Free a previously allocated descriptor set
    /// </summary>
    /// <param name="set">to free</param>
    public static void FreeDescriptorSet(DescriptorSet set)
    {
        Logger.AssertAndThrow(_descriptorSetIdTrack.Remove(set, out var id),
            "Tried to free a descriptor set which was not allocated by this handler!", nameof(DescriptorSetHandler));

        Logger.AssertAndThrow(_managedDescriptorPools.TryGetValue(id, out var pool),
            $"No descriptor pool found for descriptor set {id}", nameof(DescriptorSetHandler));

        pool.FreeDescriptorSet(set);
    }

    /// <summary>
    /// Allocate a new descriptor set
    /// </summary>
    /// <param name="descriptorSetLayoutId">Id of the descriptor set</param>
    /// <remarks>Must be registered with <see cref="RegisterDescriptorSetAttribute"/></remarks>
    public static DescriptorSet AllocateDescriptorSet(Identification descriptorSetLayoutId)
    {
        Logger.AssertAndThrow(_descriptorSetTypes.TryGetValue(descriptorSetLayoutId, out var type),
            $"Id {descriptorSetLayoutId} not present", nameof(DescriptorSetHandler));

        Logger.AssertAndThrow(type == DescriptorTrackingType.Normal,
            $"Only 'normal' descriptor sets can be allocated through {nameof(AllocateDescriptorSet)}. ID: {descriptorSetLayoutId}",
            nameof(DescriptorSetHandler));

        Logger.AssertAndThrow(_managedDescriptorPools.TryGetValue(descriptorSetLayoutId, out var pool),
            $"No descriptor pool found for descriptor set {descriptorSetLayoutId}", nameof(DescriptorSetHandler));

        return pool.AllocateDescriptorSet();
    }

    /// <summary>
    /// Allocate a new variable descriptor set
    /// </summary>
    /// <param name="descriptorSetLayoutId">Id of the descriptor set</param>
    /// <param name="count">Amount of descriptors to allocate in set</param>
    /// <remarks></remarks>
    public static DescriptorSet AllocateVariableDescriptorSet(Identification descriptorSetLayoutId, uint count)
    {
        Logger.AssertAndThrow(_descriptorSetTypes.TryGetValue(descriptorSetLayoutId, out var type),
            $"Id {descriptorSetLayoutId} not present", nameof(DescriptorSetHandler));

        Logger.AssertAndThrow(type == DescriptorTrackingType.Normal,
            $"Only 'variable' descriptor sets can be allocated through {nameof(AllocateVariableDescriptorSet)}. ID: {descriptorSetLayoutId}",
            nameof(DescriptorSetHandler));

        Logger.AssertAndThrow(_managedDescriptorPools.TryGetValue(descriptorSetLayoutId, out var pool),
            $"No descriptor pool found for descriptor set {descriptorSetLayoutId}", nameof(DescriptorSetHandler));

        return pool.AllocateVariableDescriptorSet(count);
    }

    internal static void AddExternalDescriptorSetLayout(Identification id, DescriptorSetLayout layout)
    {
        Logger.AssertAndThrow(!_descriptorSetTypes.ContainsKey(id),
            $"Id {id} already present", nameof(DescriptorSetHandler));

        _descriptorSetTypes.Add(id, DescriptorTrackingType.External);
        _externalDescriptorSetLayouts.Add(id, layout);
    }

    internal static void AddDescriptorSetLayout(Identification id, DescriptorSetLayoutBinding[] bindings,
        DescriptorBindingFlags[]? bindingFlags, DescriptorSetLayoutCreateFlags createFlags, uint descriptorSetsPerPool)
    {
        Logger.AssertAndThrow(!_descriptorSetTypes.ContainsKey(id),
            $"Id {id} already present", nameof(DescriptorSetHandler));

        Logger.AssertAndThrow(bindingFlags is null || bindingFlags.Length == bindings.Length,
            $"Binding flags length does not match bindings: {id}", nameof(DescriptorSetHandler));

        Logger.AssertAndThrow(
            bindingFlags is null ||
            bindingFlags.Any(flag => flag.HasFlag(DescriptorBindingFlags.VariableDescriptorCountBit)),
            $"Binding with variable descriptor count present. Use {nameof(DescriptorSetRegistry.RegisterExternalDescriptorSet)} for this: {id}",
            nameof(DescriptorSetHandler));

        _descriptorSetTypes.Add(id, DescriptorTrackingType.Normal);

        _managedDescriptorPools.Add(id, new ManagedDescriptorPool(bindings, bindingFlags, createFlags,
            descriptorSetsPerPool));
    }

    internal static void AddVariableDescriptorSetLayout(Identification id, DescriptorSetLayoutBinding binding,
        DescriptorBindingFlags bindingFlag, DescriptorSetLayoutCreateFlags createFlags, uint descriptorSetsPerPool)
    {
        Logger.AssertAndThrow(!_descriptorSetTypes.ContainsKey(id),
            $"Id {id} already present", nameof(DescriptorSetHandler));


        _descriptorSetTypes.Add(id, DescriptorTrackingType.Variable);

        bindingFlag |= DescriptorBindingFlags.VariableDescriptorCountBit;
        _managedDescriptorPools.Add(id, new ManagedDescriptorPool(new[] { binding }, new[] { bindingFlag }, createFlags,
            descriptorSetsPerPool));
    }


    internal static void Clear()
    {
        foreach (var pool in _managedDescriptorPools.Values)
        {
            pool.Dispose();
        }

        _managedDescriptorPools.Clear();
        _descriptorSetTypes.Clear();
        _descriptorSetIdTrack.Clear();
        _externalDescriptorSetLayouts.Clear();
    }

    /// <summary>
    ///     Get a specific descriptor set layout
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static DescriptorSetLayout GetDescriptorSetLayout(Identification id)
    {
        Logger.AssertAndThrow(_descriptorSetTypes.TryGetValue(id, out var type),
            $"Id {id} not present", nameof(DescriptorSetHandler));

        if (type == DescriptorTrackingType.External)
            return _externalDescriptorSetLayouts[id];

        Logger.AssertAndThrow(_managedDescriptorPools.TryGetValue(id, out var pool),
            $"No descriptor pool found for descriptor set {id}", nameof(DescriptorSetHandler));
        return pool.GetDescriptorSetLayout();
    }

    internal static void RemoveDescriptorSetLayout(Identification objectId)
    {
        Logger.AssertAndThrow(_descriptorSetTypes.Remove(objectId, out var type),
            $"Id {objectId} not present", nameof(DescriptorSetHandler));

        if (type == DescriptorTrackingType.External)
        {
            _externalDescriptorSetLayouts.Remove(objectId);
            return;
        }

        Logger.AssertAndThrow(_managedDescriptorPools.Remove(objectId, out var pool),
            $"No descriptor pool found for descriptor set {objectId}", nameof(DescriptorSetHandler));

        pool.Dispose();
    }

    private unsafe class ManagedDescriptorPool : IDisposable
    {
        private readonly DescriptorPoolSize[] _descriptorPoolSizes;
        private readonly DescriptorSetLayout _descriptorSetLayout;
        private uint _maxSetCount;

        private Dictionary<uint, DescriptorPool> _descriptorPools = new();
        private Dictionary<uint, (uint maxSetCount, int usedSets)> _descriptorPoolUsage = new();

        private Dictionary<DescriptorSet, (uint count, uint poolId)> _descriptorSetTrackingInfo = new();

        public ManagedDescriptorPool(ReadOnlySpan<DescriptorSetLayoutBinding> bindings,
            DescriptorBindingFlags[]? descriptorBindingFlagsArray,
            DescriptorSetLayoutCreateFlags createFlags, uint setsPerPool)
        {
            _descriptorSetLayout = CreateDescriptorSetLayout(bindings, descriptorBindingFlagsArray, createFlags);
            CalculateDescriptorPoolSize(bindings, setsPerPool, out _descriptorPoolSizes);
            _maxSetCount = setsPerPool;
        }


        private static DescriptorSetLayout CreateDescriptorSetLayout(ReadOnlySpan<DescriptorSetLayoutBinding> bindings,
            DescriptorBindingFlags[]? descriptorBindingFlagsArray, DescriptorSetLayoutCreateFlags createFlags)
        {
            descriptorBindingFlagsArray ??= Array.Empty<DescriptorBindingFlags>();
            
            fixed (DescriptorSetLayoutBinding* bindingPtr = &bindings.GetPinnableReference())
            fixed (DescriptorBindingFlags* bindingFlagsPtr = &descriptorBindingFlagsArray[0])
            {
                DescriptorSetLayoutCreateInfo createInfo = new()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    Flags = createFlags,
                    BindingCount = (uint)bindings.Length,
                    PBindings = bindingPtr,
                    PNext = descriptorBindingFlagsArray.Length > 0 ? bindingFlagsPtr : null
                };

                VulkanUtils.Assert(VulkanEngine.Vk.CreateDescriptorSetLayout(VulkanEngine.Device, createInfo,
                    VulkanEngine.AllocationCallback, out var layout));

                return layout;
            }
        }

        public DescriptorSet AllocateVariableDescriptorSet(uint count)
        {
            var poolId = GetAvailableDescriptorPoolId(count);
            var pool = GetDescriptorPool(poolId);

            var layout = _descriptorSetLayout;

            DescriptorSetVariableDescriptorCountAllocateInfo variableAllocateInfo = new()
            {
                SType = StructureType.DescriptorSetVariableDescriptorCountAllocateInfo,
                DescriptorSetCount = 1,
                PDescriptorCounts = &count
            };

            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = pool,
                DescriptorSetCount = 1,
                PSetLayouts = &layout,
                PNext = &variableAllocateInfo
            };

            VulkanUtils.Assert(VulkanEngine.Vk.AllocateDescriptorSets(VulkanEngine.Device, allocateInfo, out var set));
            _descriptorSetTrackingInfo.Add(set, (count, poolId));
            IncreaseUseCount(poolId, count);

            return set;
        }

        public DescriptorSet AllocateDescriptorSet()
        {
            var poolId = GetAvailableDescriptorPoolId(1);
            var pool = GetDescriptorPool(poolId);

            var layout = _descriptorSetLayout;

            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = pool,
                DescriptorSetCount = 1,
                PSetLayouts = &layout
            };

            VulkanUtils.Assert(VulkanEngine.Vk.AllocateDescriptorSets(VulkanEngine.Device, allocateInfo, out var set));
            _descriptorSetTrackingInfo.Add(set, (1, poolId));
            IncreaseUseCount(poolId, 1);
            return set;
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public void FreeDescriptorSet(DescriptorSet set)
        {
            if (!_descriptorSetTrackingInfo.Remove(set, out var trackingInfo))
                return;

            var (count, poolId) = trackingInfo;
            var pool = GetDescriptorPool(poolId);

            VulkanUtils.Assert(VulkanEngine.Vk.FreeDescriptorSets(VulkanEngine.Device, pool, 1, set));

            DecreaseUseCount(poolId, count);
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

        private uint GetAvailableDescriptorPoolId(uint availableSets)
        {
            if (availableSets > _maxSetCount)
            {
                _maxSetCount = availableSets * 12;
            }

            var searchResults = _descriptorPoolUsage.Where(
                entry => entry.Value.maxSetCount - entry.Value.usedSets >= availableSets).ToArray();

            return searchResults.Length != 0 ? searchResults[0].Key : CreateDescriptorPool();
        }

        public void IncreaseUseCount(uint poolId, uint usedSets)
        {
            var (maxSetCount, currentUsedSets) = _descriptorPoolUsage[poolId];
            _descriptorPoolUsage[poolId] = (maxSetCount, currentUsedSets + (int)usedSets);
        }

        public void DecreaseUseCount(uint poolId, uint usedSets)
        {
            var (maxSetCount, currentUsedSets) = _descriptorPoolUsage[poolId];
            _descriptorPoolUsage[poolId] = (maxSetCount, currentUsedSets - (int)usedSets);
        }

        private uint CreateDescriptorPool()
        {
            var poolSizes = _descriptorPoolSizes.AsSpan();
            var poolSizeHandle = GCHandle.Alloc(_descriptorPoolSizes, GCHandleType.Pinned);


            DescriptorPoolCreateInfo createInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = (DescriptorPoolSize*)Unsafe.AsPointer(ref poolSizes.GetPinnableReference()),
                MaxSets = _maxSetCount,
                Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit
            };

            VulkanUtils.Assert(VulkanEngine.Vk.CreateDescriptorPool(
                VulkanEngine.Device, createInfo, VulkanEngine.AllocationCallback, out var pool));

            poolSizeHandle.Free();

            var poolId = (uint)_descriptorPools.Count;
            _descriptorPools.Add(poolId, pool);
            _descriptorPoolUsage.Add(poolId, (_maxSetCount, 0));

            return poolId;
        }

        private DescriptorPool GetDescriptorPool(uint poolId)
        {
            return _descriptorPools[poolId];
        }

        public void Dispose()
        {
            VulkanEngine.Vk.DestroyDescriptorSetLayout(VulkanEngine.Device, _descriptorSetLayout,
                VulkanEngine.AllocationCallback);

            foreach (var pool in _descriptorPools.Values)
            {
                VulkanEngine.Vk.DestroyDescriptorPool(VulkanEngine.Device, pool, VulkanEngine.AllocationCallback);
            }
        }

        public DescriptorSetLayout GetDescriptorSetLayout()
        {
            return _descriptorSetLayout;
        }
    }
}