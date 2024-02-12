using System;
using JetBrains.Annotations;

namespace MintyCore;

/// <summary>
/// Represents the state of mods in the game.
/// </summary>
[PublicAPI]
[Flags]
public enum ModState

{
    /// <summary/>
    Invalid = 0,

    /// <summary/>
    RootModsOnly = 1 << 0,

    /// <summary/>
    GameModsOnly = 1 << 1,

    /// <summary/>
    AllMods = RootModsOnly | GameModsOnly
}