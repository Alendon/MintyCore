using System;
using MintyCore.Utils;

namespace MintyCore.Render.VulkanObjects;

public abstract class VulkanObject : IDisposable
{
    private bool _isDisposed;

    protected VulkanObject(IVulkanEngine vulkanEngine)
    {
        _isDisposed = false;
        VulkanEngine = vulkanEngine;
    }
    
    protected VulkanObject(IVulkanEngine vulkanEngine, IAllocationTracker? allocationTracker)
    {
        _isDisposed = false;
        VulkanEngine = vulkanEngine;
        _allocationTracker = allocationTracker;
        
        _allocationTracker?.TrackAllocation(this);
    }

    public bool IsDisposed => _isDisposed;

    protected internal IAllocationTracker? _allocationTracker { get; private set; }
    protected internal IVulkanEngine VulkanEngine { get; private set; }

    protected virtual void ReleaseUnmanagedResources()
    {
    }

    protected virtual void ReleaseManagedResources()
    {
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        ReleaseUnmanagedResources();
        if (!disposing) return;
        
        _allocationTracker?.RemoveAllocation(this);
        ReleaseManagedResources();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~VulkanObject()
    {
        Dispose(false);
    }
}