using System;

namespace MintyCore.Utils;

public class EmptyDisposable : IDisposable
{
    public static EmptyDisposable Instance { get; } = new EmptyDisposable();
    
    public void Dispose()
    {
        
    }
}