using System.Diagnostics;
using JetBrains.Annotations;

namespace MintyCore.Utils;

/// <summary>
/// Helper class to manage time.
/// </summary>
[PublicAPI]
public class Timer
{
    /// <summary>
    /// How often per second a frame should be rendered.
    /// </summary>
    public int TargetFps { get; set; } = 120;
    
    /// <summary>
    /// 
    /// </summary>
    public int RealFps { get; private set; }

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
    private int _accumulatedTicks;
    private float _accumulatedTicksTime;
    
    private int _accumulatedFrames;
    private float _accumulatedFramesTime;

    /// <summary>
    /// Constructor
    /// </summary>
    public Timer()
    {
        _stopwatch = Stopwatch.StartNew();
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
        _accumulatedFrames = 0;
        _accumulatedFramesTime = 0f;
    }

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
    /// Whether or not the next frame should be rendered.
    /// </summary>
    /// <returns></returns>
    public bool RenderUpdate(out float deltaTime, bool increaseRenderCount = true)
    {
        var frameTime = 1f / TargetFps;
        
        if (PassedRealTime < frameTime)
        {
            deltaTime = 0f;
            return false;
        }
        deltaTime = PassedRealTime;

        PassedRealTime = 0f;

        if (!increaseRenderCount) return true;
        
        _accumulatedFrames++;
        _accumulatedFramesTime += deltaTime;

        if (_accumulatedFramesTime < 1f) return true;
        
        RealFps = _accumulatedFrames;
        _accumulatedFrames = 0;
        _accumulatedFramesTime -= 1f;
        
        Logger.WriteLog($"FPS: {RealFps}, delta: {deltaTime}", LogImportance.Debug, "Timer");

        return true;
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