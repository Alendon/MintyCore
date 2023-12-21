using System;

namespace MintyCore.Utils;

public struct DisposeActionWrapper : IDisposable
{
    private Action _action;
    
    public DisposeActionWrapper(Action action)
    {
        _action = action;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _action?.Invoke();
    }
}