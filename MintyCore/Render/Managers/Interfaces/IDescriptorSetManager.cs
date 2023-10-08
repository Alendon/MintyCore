using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.Managers.Interfaces;

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

    void AddExternalDescriptorSetLayout(Identification id, DescriptorSetLayout layout);

    void AddDescriptorSetLayout(Identification id, DescriptorSetLayoutBinding[] bindings,
        DescriptorBindingFlags[]? bindingFlags, DescriptorSetLayoutCreateFlags createFlags, uint descriptorSetsPerPool);

    void AddVariableDescriptorSetLayout(Identification id, DescriptorSetLayoutBinding binding,
        DescriptorBindingFlags bindingFlag, DescriptorSetLayoutCreateFlags createFlags, uint descriptorSetsPerPool);

    void Clear();

    /// <summary>
    ///     Get a specific descriptor set layout
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    DescriptorSetLayout GetDescriptorSetLayout(Identification id);

    void RemoveDescriptorSetLayout(Identification objectId);
}