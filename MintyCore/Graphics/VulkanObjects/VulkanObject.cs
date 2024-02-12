using System;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Graphics.VulkanObjects;

/// <summary>
/// Represents a base class for Vulkan objects.
/// </summary>
[PublicAPI]
public abstract class VulkanObject : IDisposable
{
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the VulkanObject class.
    /// </summary>
    /// <param name="vulkanEngine">The Vulkan engine.</param>
    protected VulkanObject(IVulkanEngine vulkanEngine)
    {
        _isDisposed = false;
        VulkanEngine = vulkanEngine;
    }

    /// <summary>
    /// Initializes a new instance of the VulkanObject class with allocation tracking.
    /// </summary>
    /// <param name="vulkanEngine">The Vulkan engine.</param>
    /// <param name="allocationHandler">The allocation handler.</param>
    protected VulkanObject(IVulkanEngine vulkanEngine, IAllocationHandler? allocationHandler)
    {
        _isDisposed = false;
        VulkanEngine = vulkanEngine;
        AllocationHandler = allocationHandler;

        AllocationHandler?.TrackAllocation(this);
    }

    /// <summary>
    /// Gets a value indicating whether this instance is disposed.
    /// </summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>
    /// Gets the allocation handler.
    /// </summary>
    protected internal IAllocationHandler? AllocationHandler { get; private set; }

    /// <summary>
    /// Gets the Vulkan engine.
    /// </summary>
    protected internal IVulkanEngine VulkanEngine { get; private set; }

    /// <summary>
    /// Releases the unmanaged resources.
    /// </summary>
    protected virtual void ReleaseUnmanagedResources()
    {
    }

    /// <summary>
    /// Releases the managed resources.
    /// </summary>
    protected virtual void ReleaseManagedResources()
    {
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        _isDisposed = true;

        ReleaseUnmanagedResources();
        if (!disposing) return;

        AllocationHandler?.RemoveAllocation(this);
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