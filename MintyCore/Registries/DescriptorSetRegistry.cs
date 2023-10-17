using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Render;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> for all <see cref="DescriptorSet" />
/// </summary>
[Registry("descriptor_set", applicableGameType: GameType.Client)]
[PublicAPI]
public class DescriptorSetRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.DescriptorSet;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    public required IDescriptorSetManager DescriptorSetManager { private get; init; }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        if (Engine.HeadlessModeActive)
            return;
        DescriptorSetManager.RemoveDescriptorSetLayout(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        DescriptorSetManager.Clear();
    }

    /// <summary>
    /// Register a descriptor set (layout)
    /// Used by the SourceGenerator for the <see cref="Registries.RegisterDescriptorSetAttribute"/>
    /// </summary>
    /// <param name="id">Id of the DescriptorSet</param>
    /// <param name="descriptorSetInfo">Info to create a descriptor set</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterDescriptorSet(Identification id, DescriptorSetInfo descriptorSetInfo)
    {
        if (Engine.HeadlessModeActive)
            return;
        DescriptorSetManager.AddDescriptorSetLayout(id, descriptorSetInfo.Bindings, descriptorSetInfo.BindingFlags,
            descriptorSetInfo.CreateFlags, descriptorSetInfo.DescriptorSetsPerPool);
    }

    /// <summary>
    /// Register a variable descriptor set (layout)
    /// </summary>
    /// <param name="id">Id of the DescriptorSet</param>
    /// <param name="descriptorSetInfo">Info to create a variable descriptor set</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterVariableDescriptorSet(Identification id, VariableDescriptorSetInfo descriptorSetInfo)
    {
        if (Engine.HeadlessModeActive)
            return;
        DescriptorSetManager.AddVariableDescriptorSetLayout(id, descriptorSetInfo.Binding,
            descriptorSetInfo.BindingFlags,
            descriptorSetInfo.CreateFlags, descriptorSetInfo.DescriptorSetsPerPool);
    }

    /// <summary>
    /// Register a externally created descriptor set (layout)
    /// </summary>
    /// <param name="id">Id of the descriptor set</param>
    /// <param name="descriptorSetInfo">Info containing the descriptor set layout</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterExternalDescriptorSet(Identification id, ExternalDescriptorSetInfo descriptorSetInfo)
    {
        if (Engine.HeadlessModeActive)
            return;
        DescriptorSetManager.AddExternalDescriptorSetLayout(id, descriptorSetInfo.Layout);
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
    public required DescriptorSetLayoutBinding[] Bindings { get; set; }

    /// <summary>
    /// Optional binding flags to use
    /// </summary>
    public DescriptorBindingFlags[]? BindingFlags { get; set; }

    /// <summary>
    /// Descriptor set create flags
    ///
    /// <see cref="DescriptorSetLayoutCreateInfo.Flags"/>
    /// </summary>
    public DescriptorSetLayoutCreateFlags CreateFlags { get; set; }

    /// <summary>
    /// Pool size for the descriptor set
    /// </summary>
    public required uint DescriptorSetsPerPool { get; set; }
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
    public required DescriptorSetLayoutBinding Binding { get; set; }

    /// <summary>
    ///    Optional binding flags to use
    /// </summary>
    public DescriptorBindingFlags BindingFlags { get; set; }

    /// <summary>
    /// Descriptor set create flags
    /// </summary>
    public DescriptorSetLayoutCreateFlags CreateFlags { get; set; }

    /// <summary>
    /// Initial pool size for the descriptor set
    /// Will be increased if needed
    /// </summary>
    public required uint DescriptorSetsPerPool { get; set; }
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
    public required DescriptorSetLayout Layout { get; set; }
}