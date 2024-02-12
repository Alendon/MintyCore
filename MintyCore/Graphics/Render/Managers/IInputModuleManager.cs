using System.Collections.Generic;
using Autofac;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers;

/// <summary>
/// Interface for managing input modules.
/// </summary>
public interface IInputModuleManager
{
    /// <summary>
    /// Registers an input data module with the given id
    /// </summary>
    /// <param name="id"> The id of the module </param>
    /// <typeparam name="TModule"> The type of the module </typeparam>
    /// <remarks>Not intended to be called by user code</remarks>
    public void RegisterInputModule<TModule>(Identification id) where TModule : InputModule;

    /// <summary>
    /// Gets the ids of the registered input modules.
    /// </summary>
    IReadOnlySet<Identification> RegisteredInputModuleIds { get; }

    /// <summary>
    /// Creates instances of the input modules.
    /// </summary>
    /// <param name="lifetimeScope">The lifetime scope of the instances.</param>
    /// <returns>A dictionary mapping the ids to the instances of the input modules.</returns>
    Dictionary<Identification, InputModule> CreateInputModuleInstances(out ILifetimeScope lifetimeScope);

    /// <summary>
    /// Sets the active status of a module.
    /// </summary>
    /// <param name="moduleId">The id of the module.</param>
    /// <param name="isActive">The active status to set.</param>
    void SetModuleActive(Identification moduleId, bool isActive);

    /// <summary>
    /// Checks if a module is active.
    /// </summary>
    /// <param name="moduleId">The id of the module.</param>
    /// <returns>True if the module is active, false otherwise.</returns>
    bool IsModuleActive(Identification moduleId);

    /// <summary>
    /// Unregisters an input module.
    /// </summary>
    /// <param name="objectId">The id of the module.</param>
    void UnRegisterInputModule(Identification objectId);

    /// <summary>
    /// Clear all internal data.
    /// </summary>
    void Clear();
}