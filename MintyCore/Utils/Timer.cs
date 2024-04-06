using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Serilog;

namespace MintyCore.Utils;

/// <summary>
/// Helper class to manage time.
/// </summary>
[PublicAPI]
public class Timer
{
    /// <summary>
    /// How often per second the game should be updated.
    /// </summary>
    public int TargetTicksPerSecond { get; set; } = 20;

    /// <summary>
    /// 
    /// </summary>
    public int RealTicksPerSecond { get; private set; }

    /// <summary>
    /// The time modifier. 1.0 is normal speed, 0.5 is half speed, 2.0 is double speed, etc.
    /// </summary>
    public float TimeScale { get; set; } = 1f;

    /// <summary>
    /// The passed real time for the current tick.
    /// </summary>
    public float PassedRealTime { get; private set; }

    /// <summary>
    /// The passed game time for the current tick.
    /// </summary>
    public float PassedGameTime { get; private set; }

    private readonly Stopwatch _stopwatch;
    private readonly Stopwatch _sinceStartStopwatch;
    private int _accumulatedTicks;
    private float _accumulatedTicksTime;

    /// <summary>
    /// Constructor
    /// </summary>
    public Timer()
    {
        _stopwatch = Stopwatch.StartNew();
        _sinceStartStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Reset the timer.
    /// </summary>
    public void Reset()
    {
        _stopwatch.Restart();
        PassedGameTime = 0f;
        PassedRealTime = 0f;
        _accumulatedTicks = 0;
        _accumulatedTicksTime = 0f;
    }
    
    /// <summary>
    /// Get the total elapsed time since the timer was started.
    /// </summary>
    public TimeSpan ElapsedTimeSinceStart => _sinceStartStopwatch.Elapsed;

    /// <summary>
    ///  Update the timer.
    /// </summary>
    public void Tick()
    {
        var passedTime = (float) _stopwatch.Elapsed.TotalSeconds;
        PassedRealTime += passedTime;
        PassedGameTime += passedTime * TimeScale;
        _stopwatch.Restart();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <param name="increaseTickCount"></param>
    /// <returns></returns>
    public bool GameUpdate(out float deltaTime, bool increaseTickCount = true)
    {
        var tickTime = 1f / TargetTicksPerSecond;

        if (PassedGameTime < tickTime)
        {
            deltaTime = 0;
            return false;
        }

        deltaTime = PassedGameTime;
        PassedGameTime = 0f;

        if (!increaseTickCount) return true;

        _accumulatedTicks++;
        _accumulatedTicksTime += deltaTime;

        if (!(_accumulatedTicksTime >= 1f)) return true;

        RealTicksPerSecond = _accumulatedTicks;
        _accumulatedTicks = 0;
        _accumulatedTicksTime -= 1f;

        return true;
    }
}