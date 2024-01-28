using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using MintyCore.Utils;
using OneOf;

namespace MintyCore.Graphics.Render;

public abstract class DictionaryInputData
{
    public abstract Type KeyType { get; }
    public abstract Type DataType { get; }
}

public class DictionaryInputData<TKey, TData> : DictionaryInputData
    where TKey : notnull
{
    public override Type KeyType => typeof(TKey);
    public override Type DataType => typeof(TData);

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
            _data[key] = data;
    }

    public void RemoveData(TKey key)
    {
        if (_lockCount > 0)
        {
            _changesWhileLocked.Enqueue(key);
            return;
        }

        lock (_data)
            _data.Remove(key);
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
        if (Interlocked.Decrement(ref _lockCount) == 0)
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
            }
    }
}