using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace MintyCore.Graphics.Render.Data;

/// <summary>
/// Abstract class for dictionary input data.
/// </summary>
public abstract class DictionaryInputData
{
    /// <summary>
    /// Gets the type of the key.
    /// </summary>
    public abstract Type KeyType { get; }

    /// <summary>
    /// Gets the type of the data.
    /// </summary>
    public abstract Type DataType { get; }

    /// <summary>
    /// Gets a value indicating whether the data was modified.
    /// </summary>
    public abstract bool WasModified { get; }

    /// <summary>
    /// Resets the modified state.
    /// </summary>
    public abstract void ResetModified();
}

public interface DictionaryInputDataSet<in TKey, in TData>
{
     public void SetData(TKey key, TData data);
}

public interface DictionaryInputDataRemove<in TKey>
{
    public void RemoveData(TKey key);
}

/// <summary>
/// Class for dictionary input data with specific key and data types.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TData">The type of the data.</typeparam>
[PublicAPI]
public class DictionaryInputData<TKey, TData>(bool alwaysModified) : DictionaryInputData, DictionaryInputDataSet<TKey, TData>, DictionaryInputDataRemove<TKey>
    where TKey : notnull
{
    /// <inheritdoc />
    public override Type KeyType => typeof(TKey);

    /// <inheritdoc />
    public override Type DataType => typeof(TData);

    private bool _wasModified;

    /// <inheritdoc />
    public override bool WasModified => _wasModified || alwaysModified;

    /// <inheritdoc />
    public override void ResetModified() => _wasModified = false;

    private readonly Dictionary<TKey, TData> _data = new();

    /// <summary>
    /// Sets the data for the given key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="data">The data.</param>
    public virtual void SetData(TKey key, TData data)
    {
        lock (_data)
        {
            _data[key] = data;
            _wasModified = true;
        }
    }

    /// <summary>
    /// Removes the data for the given key.
    /// </summary>
    /// <param name="key">The key.</param>
    public virtual void RemoveData(TKey key)
    {
        lock (_data)
        {
            _data.Remove(key);
            _wasModified = true;
        }
    }

    /// <summary>
    /// Acquires the data as a read-only dictionary.
    /// </summary>
    /// <returns>A read-only dictionary containing the data.</returns>
    public virtual ReadOnlyDictionary<TKey, TData> AcquireData()
    {
        lock (_data)
        {
            //create a readonly copy of the dictionary
            return new ReadOnlyDictionary<TKey, TData>(new Dictionary<TKey, TData>(_data));
        }
    }
}