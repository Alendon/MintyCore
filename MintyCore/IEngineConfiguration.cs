using System.Collections.Generic;
using System.Threading;
using MintyCore.Utils;

namespace MintyCore;

public interface IEngineConfiguration
{
    /// <summary>
    /// If true the engine will start without all graphics features. (Console only, no window, no vulkan)
    /// </summary>
    bool HeadlessModeActive { get; }
    
    ushort HeadlessPort { get; }
    
    Thread MainThread { get; }
    
    ModState ModState { get; }
    
    bool TestingModeActive { get; }
    
    GameType GameType { get; }
    
    IReadOnlyList<string> CommandLineArguments { get; }
    
    Identification DefaultGameState { get; set; }
    Identification DefaultHeadlessGameState { get; set; }

    void SetGameType(GameType gameType);
    IEnumerable<string> GetCommandLineValues(string key, char? separator = null);
    bool HasCommandLineValue(string key);
}