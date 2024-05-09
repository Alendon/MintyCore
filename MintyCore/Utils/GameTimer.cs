using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace MintyCore.Utils;

/// <summary>
/// Helper class to manage time.
/// </summary>
[Singleton<IGameTimer>]
internal class GameTimer : IGameTimer
{
    /// <summary>
    /// The current tick count.
    /// </summary>
    public ulong Tick { get; private set; }

    /// <summary>
    /// The time passed since the last tick
    /// </summary>
    /// <remarks>Is 0 if it is not a simulation tick</remarks>
    public float DeltaTime { get; private set; }

    /// <summary>
    /// The scale at which time is passing
    /// </summary>
    public float TimeScale { get; private set; } = 1;

    /// <summary>
    /// The target number of ticks per second
    /// </summary>
    public int TargetTicksPerSecond { get; private set; } = 20;

    /// <summary>
    /// The real ticks per second
    /// </summary>
    public int TicksPerSecond { get; private set; }

    /// <summary>
    /// Indicates if the current tick is a simulation tick
    /// </summary>
    public bool IsSimulationTick { get; private set; }

    private float MinTimePerTick => 1f / TargetTicksPerSecond;

    /// <summary>
    /// Get the total elapsed time since the timer was started.
    /// </summary>
    public TimeSpan ElapsedTimeSinceStart => _sinceStartStopwatch.Elapsed;


    private readonly Stopwatch _stopwatch;
    private readonly Stopwatch _sinceStartStopwatch;
    private int _accumulatedTicks;
    private float _accumulatedTicksTime;

    /// <summary>
    /// Constructor
    /// </summary>
    public GameTimer()
    {
        _stopwatch = Stopwatch.StartNew();
        _sinceStartStopwatch = Stopwatch.StartNew();
    }


    /// <summary>
    /// Update the timer
    /// </summary>
    public void Update()
    {
        var passedRealTime = (float)_stopwatch.Elapsed.TotalSeconds;
        var passedGameTime = passedRealTime * TimeScale;

        if (passedGameTime < MinTimePerTick)
        {
            IsSimulationTick = false;
            DeltaTime = 0;
            return;
        }

        IsSimulationTick = true;
        DeltaTime = passedGameTime;
        Tick++;

        _accumulatedTicks++;
        _accumulatedTicksTime += passedRealTime;

        if (_accumulatedTicksTime >= 1)
        {
            TicksPerSecond = _accumulatedTicks;

            _accumulatedTicks = 0;
            _accumulatedTicksTime %= 1;
        }

        _stopwatch.Restart();
    }

    /// <summary>
    /// Sets the target number of ticks per second.
    /// </summary>
    public void SetTargetTicksPerSecond(int value) => TargetTicksPerSecond = value;

    /// <summary>
    /// Sets the scale at which time is passing.
    /// </summary>
    public void SetTimeScale(float value) => TimeScale = value;


    /// <summary>
    /// Reset the timer.
    /// </summary>
    public void Reset()
    {
        _stopwatch.Restart();
        _accumulatedTicks = 0;
        _accumulatedTicksTime = 0f;

        Tick = 0;
        DeltaTime = 0;
        TimeScale = 1;
    }
}