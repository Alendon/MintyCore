using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.Modding;

//TODO Add conditional behaviour to allow distinction between client server and local registries

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

    /// <summary>
    ///     Method which get executed before the main registry
    /// </summary>
    void PreRegister();
    
    void PreRegister(RegistryPhase currentPhase) { }
    
    void PostRegister(RegistryPhase currentPhase) { }

    /// <summary>
    ///     Main registry method
    /// </summary>
    void Register();

    /// <summary>
    ///     Method which get executed after the main registry
    /// </summary>
    void PostRegister();

    /// <summary>
    /// Gets called before unregistering
    /// </summary>
    void PreUnRegister();

    /// <summary>
    ///     Unregister a previous registered Object
    ///     Removes/Free all previously allocated data
    /// </summary>
    /// <param name="objectId">Id of the object to remove</param>
    void UnRegister(Identification objectId);

    /// <summary>
    /// Gets called after unregistering
    /// </summary>
    void PostUnRegister();

    /// <summary>
    ///     Clear the registry. (Reset all registry events and dispose all created resources)
    /// </summary>
    void Clear();

    /// <summary>
    ///     Clear the registry events.
    /// </summary>
    void ClearRegistryEvents();
}