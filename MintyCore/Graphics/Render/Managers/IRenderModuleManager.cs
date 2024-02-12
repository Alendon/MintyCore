using System.Collections.Generic;
using Autofac;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers;

/// <summary>
/// Interface for managing render modules in the application.
/// </summary>
public interface IRenderModuleManager
{
    /// <summary>
    /// Registers a new render module with the given identification.
    /// </summary>
    /// <typeparam name="TRenderModule">The type of the render module.</typeparam>
    /// <param name="identification">The identification of the render module.</param>
    public void RegisterRenderModule<TRenderModule>(Identification identification) where TRenderModule : RenderModule;

    /// <summary>
    /// Gets the identifications of all registered render modules.
    /// </summary>
    IReadOnlySet<Identification> RegisteredRenderModuleIds { get; }

    /// <summary>
    /// Creates instances of all active render modules and returns them along with their identifications.
    /// </summary>
    /// <param name="lifetimeScope">The lifetime scope for the created instances.</param>
    /// <returns>A dictionary mapping identifications to render module instances.</returns>
    Dictionary<Identification, RenderModule> CreateRenderModuleInstances(out ILifetimeScope lifetimeScope);

    /// <summary>
    /// Sets the active state of a render module.
    /// </summary>
    /// <param name="moduleId">The identification of the render module.</param>
    /// <param name="isActive">The new active state of the render module.</param>
    void SetModuleActive(Identification moduleId, bool isActive);

    /// <summary>
    /// Checks if a render module is active.
    /// </summary>
    /// <param name="moduleId">The identification of the render module.</param>
    /// <returns>True if the render module is active, false otherwise.</returns>
    bool IsModuleActive(Identification moduleId);

    /// <summary>
    /// Unregisters a render module.
    /// </summary>
    /// <param name="objectId">The identification of the render module.</param>
    void UnRegisterRenderModule(Identification objectId);

    /// <summary>
    /// Clears all internal data.
    /// </summary>
    void Clear();
}