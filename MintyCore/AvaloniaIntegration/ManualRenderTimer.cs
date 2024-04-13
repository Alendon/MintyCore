using System;
using System.Diagnostics;
using Avalonia.Rendering;

namespace MintyCore.AvaloniaIntegration;

/// <inheritdoc />
public class ManualRenderTimer : IRenderTimer
{
    /// <inheritdoc />
    public bool RunsInBackground => false;

    /// <inheritdoc />
    public event Action<TimeSpan>? Tick;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    
    /// <summary>
    /// Manually triggers a tick event.
    /// </summary>
    public void TriggerTick()
    {
        Tick?.Invoke(_stopwatch.Elapsed);
        _stopwatch.Restart();
    }
}