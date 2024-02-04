using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using MintyCore.Registries;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.Utils;
using MintyCore.Utils;
using Serilog;
using Silk.NET.Vulkan;

namespace MintyCore.Render.Managers;

/// <summary>
///     Class to handle the creation and destruction of <see cref="DescriptorSet" />
/// </summary>
[PublicAPI]
[Singleton<IDescriptorSetManager>(SingletonContextFlags.NoHeadless)]
internal class DescriptorSetManager : IDescriptorSetManager
{
    private readonly Dictionary<Identification, DescriptorTrackingType> _descriptorSetTypes = new();
    private readonly Dictionary<Identification, ManagedDescriptorPool> _managedDescriptorPools = new();
    private readonly Dictionary<DescriptorSet, Identification> _descriptorSetIdTrack = new();

    private readonly Dictionary<Identification, DescriptorSetLayout> _externalDescriptorSetLayouts = new();
    
    public required IVulkanEngine VulkanEngine { private get; init; }

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
    public void FreeDescriptorSet(DescriptorSet set)
    {
        if (set.Handle == default)
            return;

        if (!_descriptorSetIdTrack.Remove(set, out var id)) return;
        
        if (!_managedDescriptorPools.TryGetValue(id, out var pool))
            throw new MintyCoreException($"No descriptor pool found for descriptor set {id}");
        pool.FreeDescriptorSet(set);
    }

    /// <summary>
    /// Allocate a new descriptor set
    /// </summary>
    /// <param name="descriptorSetLayoutId">Id of the descriptor set</param>
    /// <remarks>Must be registered with <see cref="RegisterDescriptorSetAttribute"/></remarks>
    public DescriptorSet AllocateDescriptorSet(Identification descriptorSetLayoutId)
    {
        if(!_descriptorSetTypes.TryGetValue(descriptorSetLayoutId, out var type))
            throw new MintyCoreException($"Id {descriptorSetLayoutId} not present");
        
        if(type != DescriptorTrackingType.Normal)
            throw new MintyCoreException($"Only 'normal' descriptor sets can be allocated through {nameof(AllocateDescriptorSet)}. ID: {descriptorSetLayoutId}");

        if (!_managedDescriptorPools.TryGetValue(descriptorSetLayoutId, out var pool))
            throw new MintyCoreException($"No descriptor pool found for descriptor set {descriptorSetLayoutId}");
        
        var descriptorSet = pool.AllocateDescriptorSet();
        _descriptorSetIdTrack.Add(descriptorSet, descriptorSetLayoutId);
        return descriptorSet;
    }

    /// <summary>
    /// Allocate a new variable descriptor set
    /// </summary>
    /// <param name="descriptorSetLayoutId">Id of the descriptor set</param>
    /// <param name="count">Amount of descriptors to allocate in set</param>
    /// <remarks></remarks>
    public DescriptorSet AllocateVariableDescriptorSet(Identification descriptorSetLayoutId, uint count)
    {
        if(!_descriptorSetTypes.TryGetValue(descriptorSetLayoutId, out var type))
            throw new MintyCoreException($"Id {descriptorSetLayoutId} not present");
        
        if (type != DescriptorTrackingType.Variable)
            throw new MintyCoreException($"Only 'variable' descriptor sets can be allocated through {nameof(AllocateVariableDescriptorSet)}." +
                                         $" ID: {descriptorSetLayoutId}");
        
        if (!_managedDescriptorPools.TryGetValue(descriptorSetLayoutId, out var pool))
            throw new MintyCoreException($"No descriptor pool found for descriptor set {descriptorSetLayoutId}");

        var descriptorSet = pool.AllocateVariableDescriptorSet(count);
        _descriptorSetIdTrack.Add(descriptorSet, descriptorSetLayoutId);
        return descriptorSet;
    }

    /// <inheritdoc />
    public void AddExternalDescriptorSetLayout(Identification id, DescriptorSetLayout layout)
    {
        if (!_descriptorSetTypes.TryAdd(id, DescriptorTrackingType.External))
            throw new MintyCoreException($"Id {id} already present");

        _externalDescriptorSetLayouts.Add(id, layout);
    }

    /// <inheritdoc />
    public void AddDescriptorSetLayout(Identification id, DescriptorSetLayoutBinding[] bindings,
        DescriptorBindingFlags[]? bindingFlags, DescriptorSetLayoutCreateFlags createFlags, uint descriptorSetsPerPool)
    {
        if (_descriptorSetTypes.ContainsKey(id))
            throw new MintyCoreException($"Id {id} already present");

        Logger.AssertAndThrow(bindingFlags is null || bindingFlags.Length == bindings.Length,
            $"Binding flags length does not match bindings: {id}", nameof(DescriptorSetManager));
        
        Logger.AssertAndThrow(
            bindingFlags is null ||
            Array.Exists(bindingFlags, flag => !flag.HasFlag(DescriptorBindingFlags.VariableDescriptorCountBit)),
            $"Binding with variable descriptor count present. Use {nameof(DescriptorSetRegistry.RegisterExternalDescriptorSet)} for this: {id}",
            nameof(DescriptorSetManager));

        _descriptorSetTypes.Add(id, DescriptorTrackingType.Normal);

        _managedDescriptorPools.Add(id, new ManagedDescriptorPool(bindings, bindingFlags, createFlags,
            descriptorSetsPerPool, VulkanEngine));
    }

    /// <inheritdoc />
    public void AddVariableDescriptorSetLayout(Identification id, DescriptorSetLayoutBinding binding,
        DescriptorBindingFlags bindingFlag, DescriptorSetLayoutCreateFlags createFlags, uint descriptorSetsPerPool)
    {
        if(_descriptorSetTypes.ContainsKey(id))
            throw new MintyCoreException($"Id {id} already present");

        _descriptorSetTypes.Add(id, DescriptorTrackingType.Variable);

        bindingFlag |= DescriptorBindingFlags.VariableDescriptorCountBit;
        _managedDescriptorPools.Add(id, new ManagedDescriptorPool(new[] { binding }, new[] { bindingFlag }, createFlags,
            descriptorSetsPerPool, VulkanEngine));
    }


    /// <inheritdoc />
    public void Clear()
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
    public DescriptorSetLayout GetDescriptorSetLayout(Identification id)
    {
        if (!_descriptorSetTypes.TryGetValue(id, out var type))
            throw new MintyCoreException($"Id {id} not present");
        
        if (type == DescriptorTrackingType.External)
            return _externalDescriptorSetLayouts[id];
        
        if (!_managedDescriptorPools.TryGetValue(id, out var pool))
            throw new MintyCoreException($"No descriptor pool found for descriptor set {id}");
        
        return pool.GetDescriptorSetLayout();
    }

    /// <inheritdoc />
    public void RemoveDescriptorSetLayout(Identification objectId)
    {
        if (!_descriptorSetTypes.Remove(objectId, out var type))
            throw new MintyCoreException($"Id {objectId} not present");

        if (type == DescriptorTrackingType.External)
        {
            _externalDescriptorSetLayouts.Remove(objectId);
            return;
        }
        
        if (!_managedDescriptorPools.Remove(objectId, out var pool))
            throw new MintyCoreException($"No descriptor pool found for descriptor set {objectId}");
        
        pool.Dispose();
    }

    private sealed unsafe class ManagedDescriptorPool : IDisposable
    {
        private readonly DescriptorPoolSize[] _descriptorPoolSizes;
        private readonly DescriptorPoolCreateFlags _descriptorPoolFlags;
        private readonly DescriptorSetLayout _descriptorSetLayout;
        private uint _maxSetCount;

        private readonly Dictionary<uint, DescriptorPool> _descriptorPools = new();
        private readonly Dictionary<uint, (uint maxSetCount, int usedSets)> _descriptorPoolUsage = new();

        private readonly Dictionary<DescriptorSet, (uint count, uint poolId)> _descriptorSetTrackingInfo = new();
        
        private IVulkanEngine VulkanEngine { get; }

        public ManagedDescriptorPool(ReadOnlySpan<DescriptorSetLayoutBinding> bindings,
            DescriptorBindingFlags[]? descriptorBindingFlagsArray,
            DescriptorSetLayoutCreateFlags createFlags, uint setsPerPool, IVulkanEngine vulkanEngine)
        {
            _maxSetCount = setsPerPool;
            VulkanEngine = vulkanEngine;
            
            _descriptorSetLayout = CreateDescriptorSetLayout(bindings, descriptorBindingFlagsArray, createFlags);
            CalculateDescriptorPoolSize(bindings, setsPerPool, out _descriptorPoolSizes);

            _descriptorPoolFlags = DescriptorPoolCreateFlags.FreeDescriptorSetBit;

            if (createFlags.HasFlag(DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit))
            {
                _descriptorPoolFlags |= DescriptorPoolCreateFlags.UpdateAfterBindBit;
            }

            if (createFlags.HasFlag(DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBitExt))
            {
                _descriptorPoolFlags |= DescriptorPoolCreateFlags.UpdateAfterBindBitExt;
            }

            if (createFlags.HasFlag(DescriptorSetLayoutCreateFlags.HostOnlyPoolBitExt))
            {
                _descriptorPoolFlags |= DescriptorPoolCreateFlags.HostOnlyBitExt;
            }

            if (createFlags.HasFlag(DescriptorSetLayoutCreateFlags.HostOnlyPoolBitValve))
            {
                _descriptorPoolFlags |= DescriptorPoolCreateFlags.HostOnlyBitValve;
            }
        }


        private DescriptorSetLayout CreateDescriptorSetLayout(ReadOnlySpan<DescriptorSetLayoutBinding> bindings,
            DescriptorBindingFlags[]? descriptorBindingFlagsArray, DescriptorSetLayoutCreateFlags createFlags)
        {
            descriptorBindingFlagsArray ??= Array.Empty<DescriptorBindingFlags>();

            fixed (DescriptorSetLayoutBinding* bindingPtr = &bindings.GetPinnableReference())
            fixed (DescriptorBindingFlags* bindingFlagsPtr =
                       &descriptorBindingFlagsArray.AsSpan().GetPinnableReference())
            {
                DescriptorSetLayoutBindingFlagsCreateInfo flagsCreateInfo = new()
                {
                    SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
                    BindingCount = (uint)descriptorBindingFlagsArray.Length,
                    PBindingFlags = bindingFlagsPtr
                };

                DescriptorSetLayoutCreateInfo createInfo = new()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    Flags = createFlags,
                    BindingCount = (uint)bindings.Length,
                    PBindings = bindingPtr,
                    PNext = descriptorBindingFlagsArray.Length != 0 ? &flagsCreateInfo : null
                };

                VulkanUtils.Assert(VulkanEngine.Vk.CreateDescriptorSetLayout(VulkanEngine.Device, createInfo,
                    null, out var layout));

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

        private void CalculateDescriptorPoolSize(ReadOnlySpan<DescriptorSetLayoutBinding> bindings,
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
                Flags = _descriptorPoolFlags
            };

            VulkanUtils.Assert(VulkanEngine.Vk.CreateDescriptorPool(
                VulkanEngine.Device, createInfo, null, out var pool));

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
                null);

            foreach (var pool in _descriptorPools.Values)
            {
                VulkanEngine.Vk.DestroyDescriptorPool(VulkanEngine.Device, pool, null);
            }
        }

        public DescriptorSetLayout GetDescriptorSetLayout()
        {
            return _descriptorSetLayout;
        }
    }
}