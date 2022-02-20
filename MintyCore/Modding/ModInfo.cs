using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
///     Struct with all needed informations about a mod
/// </summary>
public readonly struct ModInfo
{
    /// <summary>
    ///     File location of the mod
    /// </summary>
    public readonly string ModFileLocation;

    /// <summary>
    ///     The string mod identification
    /// </summary>
    public readonly string ModId;

    /// <summary>
    ///     The name of the mod
    /// </summary>
    public readonly string ModName;

    /// <summary>
    ///     The description of the mod
    /// </summary>
    public readonly string ModDescription;

    /// <summary>
    ///     The version of the mod
    /// </summary>
    public readonly ModVersion ModVersion;

    /// <summary>
    ///     The dependencies of the mod
    /// </summary>
    public readonly ModDependency[] ModDependencies;

    /// <summary>
    ///     The execution side of the mod (local = server and client mod)
    /// </summary>
    public readonly GameType ExecutionSide;

    /// <summary>
    ///     Whether or not this is a root mod
    ///     <remarks>A mod which gets automatically loaded at application startup. Useful to manipulate the main menu</remarks>
    /// </summary>
    public readonly bool IsRootMod;

    internal ModInfo(string modFileLocation, string modId, string modName, string modDescription,
        ModVersion modVersion, ModDependency[] modDependencies, GameType executionSide, bool isRootMod)
    {
        ModFileLocation = modFileLocation;
        ModId = modId;
        ModName = modName;
        ModDescription = modDescription;
        ModVersion = modVersion;
        ModDependencies = modDependencies;
        ExecutionSide = executionSide;
        IsRootMod = isRootMod;
    }
}