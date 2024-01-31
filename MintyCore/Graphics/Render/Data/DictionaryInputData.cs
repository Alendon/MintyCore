using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using MintyCore.Utils;
using OneOf;

namespace MintyCore.Graphics.Render.Data;

public abstract class DictionaryInputData
{
    public abstract Type KeyType { get; }
    public abstract Type DataType { get; }

    public abstract bool WasModified { get; }
    public abstract void ResetModified();
}

public class DictionaryInputData<TKey, TData> : DictionaryInputData
    where TKey : notnull
{
    public override Type KeyType => typeof(TKey);
    public override Type DataType => typeof(TData);

    private bool _wasModified;
    public override bool WasModified => _wasModified;
    public override void ResetModified() => _wasModified = false;

    private readonly Dictionary<TKey, TData> _data = new();
    private readonly ConcurrentQueue<OneOf<TKey, (TKey, TData)>> _changesWhileLocked = new();

    private int _lockCount = 0;

    public void SetData(TKey key, TData data)
    {
        if (_lockCount > 0)
        {
            _changesWhileLocked.Enqueue((key, data));
            return;
        }

        lock (_data)
        {
            _data[key] = data;
            _wasModified = true;
        }
    }

    public void RemoveData(TKey key)
    {
        if (_lockCount > 0)
        {
            _changesWhileLocked.Enqueue(key);
            return;
        }

        lock (_data)
        {
            _data.Remove(key);
            _wasModified = true;
        }
    }

    public DisposeActionWrapper AcquireData(out IReadOnlyDictionary<TKey, TData> data)
    {
        lock (_data)
        {
            LockOnce();
            data = _data;
            return new DisposeActionWrapper(UnlockOnce);
        }
    }

    private void LockOnce()
    {
        Interlocked.Increment(ref _lockCount);
    }

    private void UnlockOnce()
    {
        if (Interlocked.Decrement(ref _lockCount) <= 0)
            ApplyChanges();
    }

    private void ApplyChanges()
    {
        lock (_data)
            while (_changesWhileLocked.TryDequeue(out var change))
            {
                if (change.TryPickT0(out var delete, out (TKey key, TData data) update))
                    _data.Remove(delete);
                else
                    _data[update.key] = update.data;

                //dont execute outside of the loop, as this would cause the _wasModified to be set to true even if there were no changes
                _wasModified = true;
            }
    }
}