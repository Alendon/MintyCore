using System;
using System.Collections.Generic;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

/// <summary>
///     Handler which manages all <see cref="Material" /> and MaterialCollections
/// </summary>
public static class MaterialHandler
{
    private static readonly Dictionary<Identification, Material> _materials = new();
    private static readonly Dictionary<ushort, Func<Identification, DescriptorSet>> _descriptorFetch = new();
    private static readonly Dictionary<Identification, ushort> _idCategoryMap = new();

    internal static void AddMaterial(Identification materialId, Identification pipeline,
        params (Identification, uint)[] descriptorSetArr)
    {
        UnmanagedArray<(DescriptorSet, uint)> descriptorSets = new(descriptorSetArr.Length);
        for (var i = 0; i < descriptorSetArr.Length; i++)
        {
            var (descriptorId, index) = descriptorSetArr[i];
            var fetchFunc = _descriptorFetch[descriptorId.Category];
            descriptorSets[i] = (fetchFunc(descriptorId), index);
        }

        var material = new Material(materialId, PipelineHandler.GetPipeline(pipeline),
            PipelineHandler.GetPipelineLayout(pipeline), descriptorSets);
        _materials.Add(materialId, material);
    }


    /// <summary>
    ///     Get a <see cref="Material" /> by the associated <see cref="Identification" />
    /// </summary>
    public static Material GetMaterial(Identification id)
    {
        return _materials[id];
    }

    internal static void Clear()
    {
        foreach (var materialHandles in _materials.Values) materialHandles.DescriptorSets.DecreaseRefCount();

        _descriptorFetch.Clear();
        _idCategoryMap.Clear();
        _materials.Clear();
    }

    internal static void RemoveMaterial(Identification objectId)
    {
        if (_idCategoryMap.Remove(objectId, out var categoryId))
        {
            _descriptorFetch.Remove(categoryId);
            return;
        }

        if (_materials.Remove(objectId, out var material))
        {
            material.DescriptorSets.DecreaseRefCount();
        }
    }

    public static void AddDescriptorHandler(Identification id, ushort categoryId,
        Func<Identification, DescriptorSet> func)
    {
        Logger.AssertAndThrow(_descriptorFetch.TryAdd(categoryId, func),
            $"A descriptor set fetch function for category id {categoryId} is already registered", "Render");
        
        _idCategoryMap.Add(id, categoryId);
    }
}