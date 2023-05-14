using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
[PublicAPI]
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
        if (Engine.HeadlessModeActive)
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
    /// Register a descriptor set (layout)
    /// Used by the SourceGenerator for the <see cref="Registries.RegisterDescriptorSetAttribute"/>
    /// </summary>
    /// <param name="id">Id of the DescriptorSet</param>
    /// <param name="descriptorSetInfo">Info to create a descriptor set</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterDescriptorSet(Identification id, DescriptorSetInfo descriptorSetInfo)
    {
        if (Engine.HeadlessModeActive)
            return;
        DescriptorSetHandler.AddDescriptorSetLayout(id, descriptorSetInfo.Bindings, descriptorSetInfo.BindingFlags,
            descriptorSetInfo.CreateFlags, descriptorSetInfo.DescriptorSetsPerPool);
    }

    /// <summary>
    /// Register a variable descriptor set (layout)
    /// </summary>
    /// <param name="id">Id of the DescriptorSet</param>
    /// <param name="descriptorSetInfo">Info to create a variable descriptor set</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterVariableDescriptorSet(Identification id, VariableDescriptorSetInfo descriptorSetInfo)
    {
        if (Engine.HeadlessModeActive)
            return;
        DescriptorSetHandler.AddVariableDescriptorSetLayout(id, descriptorSetInfo.Binding,
            descriptorSetInfo.BindingFlags,
            descriptorSetInfo.CreateFlags, descriptorSetInfo.DescriptorSetsPerPool);
    }

    /// <summary>
    /// Register a externally created descriptor set (layout)
    /// </summary>
    /// <param name="id">Id of the descriptor set</param>
    /// <param name="descriptorSetInfo">Info containing the descriptor set layout</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterExternalDescriptorSet(Identification id, ExternalDescriptorSetInfo descriptorSetInfo)
    {
        if (Engine.HeadlessModeActive)
            return;
        DescriptorSetHandler.AddExternalDescriptorSetLayout(id, descriptorSetInfo.Layout);
    }
}

/// <summary>
/// Struct which contains the information for a descriptor set
/// </summary>
[PublicAPI]
public struct DescriptorSetInfo
{
    /// <summary>
    /// Bindings used for the descriptor set
    /// </summary>
    public required DescriptorSetLayoutBinding[] Bindings;

    /// <summary>
    /// Optional binding flags to use
    /// </summary>
    public DescriptorBindingFlags[]? BindingFlags;

    /// <summary>
    /// Descriptor set create flags
    ///
    /// <see cref="DescriptorSetLayoutCreateInfo.Flags"/>
    /// </summary>
    public DescriptorSetLayoutCreateFlags CreateFlags;

    /// <summary>
    /// Pool size for the descriptor set
    /// </summary>
    public required uint DescriptorSetsPerPool;
}

/// <summary>
/// Struct which contains the information for a variable descriptor set
/// </summary>
[PublicAPI]
public struct VariableDescriptorSetInfo
{
    /// <summary>
    /// Bindings used for the descriptor set
    /// </summary>
    public required DescriptorSetLayoutBinding Binding;

    /// <summary>
    ///    Optional binding flags to use
    /// </summary>
    public DescriptorBindingFlags BindingFlags;

    /// <summary>
    /// Descriptor set create flags
    /// </summary>
    public DescriptorSetLayoutCreateFlags CreateFlags;

    /// <summary>
    /// Initial pool size for the descriptor set
    /// Will be increased if needed
    /// </summary>
    public required uint DescriptorSetsPerPool;
}

/// <summary>
/// Struct which contains the information for a externally created descriptor set
/// </summary>
[PublicAPI]
public struct ExternalDescriptorSetInfo
{
    /// <summary>
    /// Layout of the descriptor set to add
    /// </summary>
    public required DescriptorSetLayout Layout;
}