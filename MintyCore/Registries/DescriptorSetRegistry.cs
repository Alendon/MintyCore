using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

public class DescriptorSetRegistry : IRegistry
{
    /// <summary />
    public delegate void RegisterDelegate();

    public ushort RegistryId => RegistryIDs.DescriptorSet;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    public void PreRegister()
    {
    }

    public void Register()
    {
        OnRegister();
    }

    public void PostRegister()
    {
    }

    public void Clear()
    {
        DescriptorSetHandler.Clear();
        OnRegister = delegate { };
    }
        
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
    }

    /// <summary />
    public static event RegisterDelegate OnRegister = delegate { };

    public static Identification RegisterDescriptorSet(ushort modId, string stringIdentifier,
        ReadOnlySpan<DescriptorSetLayoutBinding> bindings)
    {
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.DescriptorSet, stringIdentifier);
        DescriptorSetHandler.AddDescriptorSetLayout(id, bindings);
        return id;
    }
}