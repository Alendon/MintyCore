using System;
using System.Diagnostics;
using Avalonia.Rendering;

namespace MintyCore.AvaloniaIntegration;

public class ManualRenderTimer : IRenderTimer
{
    public bool RunsInBackground => false;
    public event Action<TimeSpan>? Tick;
    private Stopwatch _stopwatch = Stopwatch.StartNew();
    
    public void TriggerTick()
    {
        Tick?.Invoke(_stopwatch.Elapsed);
        _stopwatch.Restart();
    }
}