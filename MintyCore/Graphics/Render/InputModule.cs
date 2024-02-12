using System;
using JetBrains.Annotations;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

/// <summary>
/// Abstract class for input modules.
/// </summary>
[PublicAPI]
public abstract class InputModule : IDisposable
{
    private IInputModuleDataAccessor? _moduleDataAccessor;

    /// <summary>
    /// Sets up the input module.
    /// </summary>
    public abstract void Setup();

    /// <summary>
    /// Updates the input module with the provided command buffer.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to use for the update.</param>
    public abstract void Update(ManagedCommandBuffer commandBuffer);

    /// <summary>
    /// Gets the identification of the input module.
    /// </summary>

    public abstract Identification Identification { get; }

    /// <summary>
    /// Gets or sets the data accessor for the input module.
    /// </summary>
    /// <exception cref="MintyCoreException">Thrown when the module data accessor is not set.</exception>

    public IInputModuleDataAccessor ModuleDataAccessor
    {
        protected get => _moduleDataAccessor ?? throw new MintyCoreException("ModuleDataAccessor is not set.");
        set => _moduleDataAccessor = value;
    }

    /// <inheritdoc />
    public abstract void Dispose();
}