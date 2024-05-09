using System;

namespace MintyCore.Utils;

public interface IGameTimer
{
    /// <summary>
    /// The current tick count.
    /// </summary>
    ulong Tick { get; }

    /// <summary>
    /// The time passed since the last tick
    /// </summary>
    /// <remarks>Is 0 if it is not a simulation tick</remarks>
    float DeltaTime { get; }

    /// <summary>
    /// The scale at which time is passing
    /// </summary>
    float TimeScale { get; }

    /// <summary>
    /// The target number of ticks per second
    /// </summary>
    int TargetTicksPerSecond { get; }

    /// <summary>
    /// The real ticks per second
    /// </summary>
    int TicksPerSecond { get; }

    /// <summary>
    /// Indicates if the current tick is a simulation tick
    /// </summary>
    bool IsSimulationTick { get; }

    /// <summary>
    /// Get the total elapsed time since the timer was started.
    /// </summary>
    TimeSpan ElapsedTimeSinceStart { get; }

    /// <summary>
    /// Update the timer
    /// </summary>
    void Update();

    /// <summary>
    /// Sets the target number of ticks per second.
    /// </summary>
    void SetTargetTicksPerSecond(int value);

    /// <summary>
    /// Sets the scale at which time is passing.
    /// </summary>
    void SetTimeScale(float value);

    /// <summary>
    /// Reset the timer.
    /// </summary>
    void Reset();
}