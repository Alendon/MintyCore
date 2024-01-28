using System;

namespace MintyCore.Graphics.Render;

public abstract class SingletonInputData
{
    public abstract Type DataType { get; }
}

public class SingletonInputData<TDataType> : SingletonInputData
{
    private TDataType? _data;
    private readonly object _lock = new();

    /// <inheritdoc />
    public override Type DataType => typeof(TDataType);

    public void SetData(TDataType data)
    {
        lock (_lock)
            _data = data;
    }

    public TDataType? AquireData()
    {
        lock (_lock)
            return _data;
    }
}