using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> for all <see cref="DescriptorSet" />
/// </summary>
[Registry("descriptor_set")]
public class DescriptorSetRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.DescriptorSet;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    /// <inheritdoc />
    public void PreRegister()
    {
        OnPreRegister();
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
        OnPostRegister();
    }

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        if(Engine.HeadlessModeActive)
            return;
        DescriptorSetHandler.RemoveDescriptorSetLayout(objectId);

    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <inheritdoc />
    public void Clear()
    {
        DescriptorSetHandler.Clear();
        ClearRegistryEvents();
    }

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    /// <summary>
    ///     Register a descriptor set (layout)
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the DescriptorSet</param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the DescriptorSet</param>
    /// <param name="bindings">The bindings used for the descriptor set</param>
    /// <returns>Generated <see cref="Identification" /></returns>
    [Obsolete]
    public static Identification RegisterDescriptorSet(ushort modId, string stringIdentifier,
        ReadOnlySpan<DescriptorSetLayoutBinding> bindings)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.DescriptorSet, stringIdentifier);
        if(Engine.HeadlessModeActive)
            return id;
        DescriptorSetHandler.AddDescriptorSetLayout(id, bindings);
        return id;
    }
    
    [RegisterMethod(ObjectRegistryPhase.MAIN)]
    public static void RegisterDescriptorSet(Identification id, DescriptorSetInfo descriptorSetInfo)
    {
        if(Engine.HeadlessModeActive)
            return;
        DescriptorSetHandler.AddDescriptorSetLayout(id, descriptorSetInfo.Bindings);
    }
}

public struct DescriptorSetInfo
{
    public DescriptorSetLayoutBinding[] Bindings;
}