using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
///     Interface for all registries
/// </summary>
[PublicAPI]
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

    /// <summary>
    /// Method which gets called before the registry gets processed
    /// </summary>
    /// <param name="currentPhase"> The current registry phase </param>
    void PreRegister(ObjectRegistryPhase currentPhase)
    {
        //empty default implementation
    }

    /// <summary>
    ///  Method which gets called after the registry gets processed
    /// </summary>
    /// <param name="currentPhase"> The current registry phase </param>
    void PostRegister(ObjectRegistryPhase currentPhase)
    {
        //empty default implementation
    }

    /// <summary>
    /// Gets called before unregistering
    /// </summary>
    void PreUnRegister()
    {
        //empty default implementation
    }

    /// <summary>
    ///     Unregister a previous registered Object
    ///     Removes/Free all previously allocated data
    /// </summary>
    /// <param name="objectId">Id of the object to remove</param>
    void UnRegister(Identification objectId);

    /// <summary>
    /// Gets called after unregistering
    /// </summary>
    void PostUnRegister()
    {
        //empty default implementation
    }

    /// <summary>
    ///     Clear the registry. (Reset all registry events and dispose all created resources)
    /// </summary>
    void Clear();
}