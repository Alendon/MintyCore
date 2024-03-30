using System;
using JetBrains.Annotations;

namespace MintyCore.Utils;

/// <summary>
///    A wrapper for an <see cref="Action" /> that will be invoked when the instance is disposed
/// </summary>
[PublicAPI]
public struct DisposeActionWrapper : IDisposable
{
    private Action _action;
    private bool _disposed;

    /// <summary>
    ///  Create a new <see cref="DisposeActionWrapper" /> instance
    /// </summary>
    /// <param name="action"> The action to invoke when the instance is disposed </param>
    public DisposeActionWrapper(Action action)
    {
        _action = action;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _action?.Invoke();
    }
}