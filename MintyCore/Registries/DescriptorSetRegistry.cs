using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
/// The <see cref="IRegistry"/> for all <see cref="DescriptorSet"/>
/// </summary>
public class DescriptorSetRegistry : IRegistry
{
    /// <summary />
    public delegate void RegisterDelegate();

    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.DescriptorSet;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    /// <inheritdoc />
    public void PreRegister()
    {
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
    }

    /// <inheritdoc />
    public void Clear()
    {
        DescriptorSetHandler.Clear();
        OnRegister = delegate { };
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
    }

    /// <summary />
    public static event RegisterDelegate OnRegister = delegate { };

    /// <summary>
    /// Register a descriptor set (layout)
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the DescriptorSet</param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the DescriptorSet</param>
    /// <param name="bindings">The bindings used for the descriptor set</param>
    /// <returns>Generated <see cref="Identification"/></returns>
    public static Identification RegisterDescriptorSet(ushort modId, string stringIdentifier,
        ReadOnlySpan<DescriptorSetLayoutBinding> bindings)
    {
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.DescriptorSet, stringIdentifier);
        DescriptorSetHandler.AddDescriptorSetLayout(id, bindings);
        return id;
    }
}