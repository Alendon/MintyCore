using System;
using JetBrains.Annotations;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

[PublicAPI]
public abstract class InputModule : IDisposable
{
    private IInputModuleDataAccessor? _moduleDataAccessor;
    public abstract void Setup();
    public abstract void Update(ManagedCommandBuffer commandBuffer);
    public abstract Identification Identification { get; }

    public IInputModuleDataAccessor ModuleDataAccessor
    {
        protected get => _moduleDataAccessor ?? throw new MintyCoreException("ModuleDataAccessor is not set.");
        set => _moduleDataAccessor = value;
    }

    /// <inheritdoc />
    public abstract void Dispose();
}