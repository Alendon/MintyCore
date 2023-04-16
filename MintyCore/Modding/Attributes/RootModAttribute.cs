using System;

namespace MintyCore.Modding.Attributes;

/// <summary>
///     Defines a mod as a "Root Mod". It will automatically be loaded if found at the startup of the game.
///     This gives the mod the ability to alter things like the main menu. Only one "Root Mod" is allowed (together with
///     the main MintyCoreMod).
/// </summary>
public class RootModAttribute : Attribute
{
}