using System;

namespace MintyCore;

[Flags]
public enum ModState
{
    Invalid = 0,
    RootModsOnly = 1 << 0,
    GameModsOnly = 1 << 1,
    AllMods = RootModsOnly | GameModsOnly
}