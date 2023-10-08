﻿using System;
using System.Collections.Generic;
using System.IO;
using Autofac;
using MintyCore.Utils;

namespace MintyCore.Modding;

public interface IModManager
{
    IRegistryManager RegistryManager { get; }
    
    /// <summary>
    /// Returns the "lowest" mod lifetime scope
    /// </summary>
    internal ILifetimeScope ModLifetimeScope { get; }
    
    /// <summary>
    ///     Get all available mod infos
    /// </summary>
    /// <returns>Enumerable containing all mod infos</returns>
    IEnumerable<ModManifest> GetAvailableMods(bool latestVersionsOnly);

    /// <summary>
    ///     Load the specified mods
    /// </summary>
    void LoadGameMods(IEnumerable<ModManifest> mods);

    /// <summary>
    ///     Load the <see cref="MintyCoreMod" /> and all registered root mods
    /// </summary>
    void LoadRootMods();

    /// <summary>
    ///     Get the instance of a loaded mod
    /// </summary>
    /// <param name="modId">Id of the mod</param>
    /// <returns>Instance of the mod</returns>
    IMod GetLoadedMod(ushort modId);

    /// <summary>
    ///     Check whether or not a mod is a root mod
    /// </summary>
    /// <param name="modId">id of the mod to check</param>
    /// <returns>True if its a root mod; false if not or the mod is not present</returns>
    bool IsRootMod(ushort modId);

    void ProcessRegistry(bool loadRootMods, LoadPhase loadPhase);

    /// <summary>
    ///     Get an enumerable with all loaded mods including modId and mod instance
    /// </summary>
    IEnumerable<(string modId, Version modVersion, IMod mod)> GetLoadedMods();

    /// <summary>
    ///     Unload mods
    /// </summary>
    /// <param name="unloadRootMods">
    ///     Whether or not root mods will be unloaded. If false they get unloaded and reloaded
    ///     immediately
    /// </param>
    void UnloadMods(bool unloadRootMods);

    /// <summary>
    ///     Check if the given set of mods is compatible to the loaded mods
    /// </summary>
    /// <param name="infoAvailableMods"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    bool ModsCompatible(IEnumerable<(string modId, Version version)> infoAvailableMods);

    /// <summary>
    /// Get a stream to the requested resource file
    /// </summary>
    /// <param name="resource">Id associated with the resource file</param>
    /// <remarks>Do not forget do dispose the stream</remarks>
    /// <returns>Stream containing the information of the resource file</returns>
    Stream GetResourceFileStream(Identification resource);

    bool FileExists(ushort modId, string location);

    internal void SearchMods(IEnumerable<DirectoryInfo>? additionalModDirectories = null);
}