using JetBrains.Annotations;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Managers;

/// <summary>
/// Manages the allocation and deallocation of vulkan descriptor sets
/// </summary>
[PublicAPI]
public interface IDescriptorSetManager
{
    /// <summary>
    ///     Free a previously allocated descriptor set
    /// </summary>
    /// <param name="set">to free</param>
    void FreeDescriptorSet(DescriptorSet set);

    /// <summary>
    /// Allocate a new descriptor set
    /// </summary>
    /// <param name="descriptorSetLayoutId">Id of the descriptor set</param>
    /// <remarks>Must be registered with <see cref="RegisterDescriptorSetAttribute"/></remarks>
    DescriptorSet AllocateDescriptorSet(Identification descriptorSetLayoutId);

    /// <summary>
    /// Allocate a new variable descriptor set
    /// </summary>
    /// <param name="descriptorSetLayoutId">Id of the descriptor set</param>
    /// <param name="count">Amount of descriptors to allocate in set</param>
    /// <remarks></remarks>
    DescriptorSet AllocateVariableDescriptorSet(Identification descriptorSetLayoutId, uint count);

    /// <summary>
    /// Add a descriptor set layout which was created externally
    /// </summary>
    void AddExternalDescriptorSetLayout(Identification id, DescriptorSetLayout layout);

    /// <summary>
    ///   Add a new descriptor set layout to the manager
    /// </summary>
    /// <param name="id"> Id of the descriptor set layout</param>
    /// <param name="bindings"> Bindings of the descriptor set layout</param>
    /// <param name="bindingFlags" >Flags for each binding</param>
    /// <param name="createFlags"> Flags for the descriptor set layout</param>
    /// <param name="descriptorSetsPerPool"> Amount of descriptor sets to allocate per pool</param>
    void AddDescriptorSetLayout(Identification id, DescriptorSetLayoutBinding[] bindings,
        DescriptorBindingFlags[]? bindingFlags, DescriptorSetLayoutCreateFlags createFlags, uint descriptorSetsPerPool);

    /// <summary>
    /// Add a new descriptor set layout with variable count to the manager
    /// </summary>
    /// <param name="id"> Id of the descriptor set layout</param>
    /// <param name="binding"> Binding of the descriptor set layout</param>
    /// <param name="bindingFlag"> Flags for the binding</param>
    /// <param name="createFlags"> Flags for the descriptor set layout</param>
    /// <param name="descriptorSetsPerPool"> Amount of descriptor sets to allocate per pool</param>
    void AddVariableDescriptorSetLayout(Identification id, DescriptorSetLayoutBinding binding,
        DescriptorBindingFlags bindingFlag, DescriptorSetLayoutCreateFlags createFlags, uint descriptorSetsPerPool);

    /// <summary>
    /// Clear all internal data
    /// </summary>
    void Clear();

    /// <summary>
    ///     Get a specific descriptor set layout
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    DescriptorSetLayout GetDescriptorSetLayout(Identification id);

    /// <summary>
    /// Remove a descriptor set layout from the manager
    /// </summary>
    /// <param name="objectId"></param>
    void RemoveDescriptorSetLayout(Identification objectId);
}