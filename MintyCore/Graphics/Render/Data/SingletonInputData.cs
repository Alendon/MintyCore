using System;
using JetBrains.Annotations;

namespace MintyCore.Graphics.Render.Data;

/// <summary>
/// Abstract class for singleton input data.
/// </summary>
[PublicAPI]
public abstract class SingletonInputData
{
    /// <summary>
    /// Gets the type of the data.
    /// </summary>
    public abstract Type DataType { get; }

    /// <summary>
    /// Gets a value indicating whether the data was modified.
    /// </summary>
    public abstract bool WasModified { get; }

    /// <summary>
    /// Resets the modified status of the data.
    /// </summary>
    public abstract void ResetModified();
}

/// <summary>
/// Class for singleton input data with specific data type.
/// </summary>
/// <typeparam name="TDataType">The type of the data.</typeparam>
[PublicAPI]
public class SingletonInputData<TDataType>(bool alwaysModified) : SingletonInputData
{
    private TDataType? _data;
    private readonly object _lock = new();

    /// <inheritdoc />
    public override Type DataType => typeof(TDataType);

    private bool _wasModified;

    /// <inheritdoc />
    public override bool WasModified => _wasModified || alwaysModified;

    /// <inheritdoc />
    public override void ResetModified() => _wasModified = false;

    /// <summary>
    /// Sets the data and marks it as modified.
    /// </summary>
    /// <param name="data">The data to set.</param>
    public void SetData(TDataType data)
    {
        lock (_lock)
        {
            _data = data;
            _wasModified = true;
        }
    }

    /// <summary>
    /// Acquires the data.
    /// </summary>
    /// <returns>The data.</returns>
    public TDataType? AcquireData()
    {
        lock (_lock)
            return _data;
    }
}