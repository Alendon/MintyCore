using System;
using System.Threading;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Data;

/// <summary>
/// Abstract class for intermediate data.
/// </summary>
[PublicAPI]
public abstract class IntermediateData : IDisposable
{
    /// <summary>
    /// Clear the internal data
    /// The instance should be in the initial state after this method
    /// </summary>
    public abstract void Clear();

    /// <summary>
    /// Gets the identification of the intermediate data.
    /// </summary>
    public abstract Identification Identification { get; }

    private readonly IIntermediateDataManager? _intermediateDataManager;

    /// <summary>
    /// Gets or sets the intermediate data manager.
    /// </summary>
    public IIntermediateDataManager IntermediateDataManager
    {
        private get => _intermediateDataManager ?? throw new NullReferenceException();
        init => _intermediateDataManager = value;
    }


    private int _refCount;

    /// <summary>
    /// Copies from the previous set IntermediateData.
    /// This method should only be overridden if the IntermediateData contains references to other objects which should be shared.
    /// </summary>
    /// <param name="previousData">The previous intermediate data.</param>
    public virtual void CopyFrom(IntermediateData? previousData)
    {
    }

    /// <summary>
    /// Increases the reference count.
    /// </summary>
    public void IncreaseRefCount()
    {
        Interlocked.Increment(ref _refCount);
    }

    /// <summary>
    /// Decreases the reference count.
    /// </summary>
    public void DecreaseRefCount()
    {
        if (_refCount == 0)
        {
            throw new InvalidOperationException("The ref count can not be decreased below 0");
        }

        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            IntermediateDataManager.RecycleIntermediateData(Identification, this);
        }
    }

    /// <summary>
    /// Disposes the intermediate data.
    /// </summary>
    public abstract void Dispose();
}