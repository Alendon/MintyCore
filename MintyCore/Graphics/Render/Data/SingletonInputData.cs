using System;
using JetBrains.Annotations;

namespace MintyCore.Graphics.Render.Data;

[PublicAPI]
public abstract class SingletonInputData
{
    public abstract Type DataType { get; }

    public abstract bool WasModified { get; }
    public abstract void ResetModified();
}

[PublicAPI]
public class SingletonInputData<TDataType> : SingletonInputData
{
    private TDataType? _data;
    private readonly object _lock = new();

    /// <inheritdoc />
    public override Type DataType => typeof(TDataType);

    private bool _wasModified;
    public override bool WasModified => _wasModified;
    public override void ResetModified() => _wasModified = false;

    public void SetData(TDataType data)
    {
        lock (_lock)
        {
            _data = data;
            _wasModified = true;
        }
    }

    public TDataType? AquireData()
    {
        lock (_lock)
            return _data;
    }
}