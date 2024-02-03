using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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


    public virtual void SetData(TKey key, TData data)
    {
        lock (_data)
        {
            _data[key] = data;
            _wasModified = true;
        }
    }

    public virtual void RemoveData(TKey key)
    {
        lock (_data)
        {
            _data.Remove(key);
            _wasModified = true;
        }
    }

    public virtual ReadOnlyDictionary<TKey, TData> AcquireData()
    {
        lock (_data)
        {
            return new ReadOnlyDictionary<TKey, TData>(_data);
        }
    }
}