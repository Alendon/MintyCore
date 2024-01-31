using System;
using JetBrains.Annotations;

namespace MintyCore.Graphics.Render.Data;

[PublicAPI]
public abstract class SingletonInputData
{
    public abstract Type DataType { get; }
}

[PublicAPI]
public class SingletonInputData<TDataType> : SingletonInputData
{
    private TDataType? _data;
    private readonly object _lock = new();

    /// <inheritdoc />
    public override Type DataType => typeof(TDataType);

    public void SetData(TDataType data)
    {
        lock (_lock)
        {
            _data = data;
        }
    }

    public TDataType? AquireData()
    {
        lock (_lock)
            return _data;
    }
}