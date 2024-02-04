using System;
using System.Collections.Generic;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

public abstract class RenderModule : IDisposable
{
    private IRenderModuleDataAccessor? _moduleDataAccessor;
    
    public virtual IEnumerable<Identification> ExecuteBefore => Array.Empty<Identification>();
    public virtual IEnumerable<Identification> ExecuteAfter => Array.Empty<Identification>();
    
    public abstract void Setup();
    public abstract void Render(ManagedCommandBuffer commandBuffer);
    public abstract Identification Identification { get; }
    
    public IRenderModuleDataAccessor ModuleDataAccessor
    {
        protected get => _moduleDataAccessor ?? throw new MintyCoreException("ModuleDataAccessor is not set.");
        set => _moduleDataAccessor = value;
    }

    /// <inheritdoc />
    public abstract void Dispose();
    
}