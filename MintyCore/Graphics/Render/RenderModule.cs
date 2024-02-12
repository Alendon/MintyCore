using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

/// <summary>
/// Abstract base class for all render modules.
/// </summary>
[PublicAPI]
public abstract class RenderModule : IDisposable
{
    private IRenderModuleDataAccessor? _moduleDataAccessor;

    /// <summary>
    /// Gets the identifications of the modules that this module should execute before.
    /// </summary>
    public virtual IEnumerable<Identification> ExecuteBefore => Array.Empty<Identification>();

    /// <summary>
    /// Gets the identifications of the modules that this module should execute after.
    /// </summary>
    public virtual IEnumerable<Identification> ExecuteAfter => Array.Empty<Identification>();

    /// <summary>
    /// Sets up the render module.
    /// </summary>
    public abstract void Setup();

    /// <summary>
    /// Renders the module using the given command buffer.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to use for rendering.</param>
    public abstract void Render(ManagedCommandBuffer commandBuffer);

    /// <summary>
    /// Gets the identification of the render module.
    /// </summary>
    public abstract Identification Identification { get; }

    /// <summary>
    /// Gets or sets the data accessor for the render module.
    /// </summary>
    /// <exception cref="MintyCoreException">The data accessor is not set.</exception>
    public IRenderModuleDataAccessor ModuleDataAccessor
    {
        protected get => _moduleDataAccessor ?? throw new MintyCoreException("ModuleDataAccessor is not set.");
        set => _moduleDataAccessor = value;
    }

    /// <inheritdoc />
    public abstract void Dispose();
}