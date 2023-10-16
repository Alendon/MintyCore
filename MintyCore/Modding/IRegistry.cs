using System.Collections.Generic;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
///     Interface for all registries
/// </summary>
public interface IRegistry
{
    /// <summary>
    ///     The id of the registry
    /// </summary>
    ushort RegistryId { get; }

    /// <summary>
    ///     Collection of registries which need to be processed before this
    /// </summary>
    IEnumerable<ushort> RequiredRegistries { get; }
    
    void PreRegister(ObjectRegistryPhase currentPhase) { }
    
    void PostRegister(ObjectRegistryPhase currentPhase) { }

    /// <summary>
    /// Gets called before unregistering
    /// </summary>
    void PreUnRegister() { }

    /// <summary>
    ///     Unregister a previous registered Object
    ///     Removes/Free all previously allocated data
    /// </summary>
    /// <param name="objectId">Id of the object to remove</param>
    void UnRegister(Identification objectId);

    /// <summary>
    /// Gets called after unregistering
    /// </summary>
    void PostUnRegister() { }

    /// <summary>
    ///     Clear the registry. (Reset all registry events and dispose all created resources)
    /// </summary>
    void Clear();
}