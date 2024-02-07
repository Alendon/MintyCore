using System;
using System.Threading;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Data;

[PublicAPI]
public abstract class IntermediateData : IDisposable
{
    /// <summary>
    /// Clear the internal data
    /// The instance should be in the initial state after this method
    /// </summary>
    public abstract void Clear();

    public AccessMode AccessMode { get; set; }
    public abstract Identification Identification { get; }

    private readonly IIntermediateDataManager? _intermediateDataManager;

    public IIntermediateDataManager IntermediateDataManager
    {
        private get => _intermediateDataManager ?? throw new NullReferenceException();
        init => _intermediateDataManager = value;
    }


    private int _refCount;

    /// <summary>
    /// Copy from the previous set IntermediateData
    /// This method should only be overridden if the IntermediateData contains references to other objects which should be shared
    /// </summary>
    /// <param name="previousData"></param>
    public virtual void CopyFrom(IntermediateData? previousData)
    {
    }

    public void IncreaseRefCount()
    {
        Interlocked.Increment(ref _refCount);
    }

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

    public abstract void Dispose();
}