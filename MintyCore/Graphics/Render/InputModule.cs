using System;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render;

public abstract class InputModule : IDisposable
{
    private IInputModuleDataAccessor? _moduleDataAccessor;
    public abstract void Setup();
    public abstract void Update(CommandBuffer commandBuffer);
    public abstract Identification Identification { get; }

    public IInputModuleDataAccessor ModuleDataAccessor
    {
        protected get => _moduleDataAccessor ?? throw new MintyCoreException("ModuleDataAccessor is not set.");
        set => _moduleDataAccessor = value;
    }

    /// <inheritdoc />
    public abstract void Dispose();
}