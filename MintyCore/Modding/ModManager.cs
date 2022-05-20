using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
///     Class which handles mod indexing, loading and unloading.
///     Also additional mod related functions
/// </summary>
public static class ModManager
{
    /// <summary>
    ///     The maximum tries to trigger and wait for the garbage collector to collect unneeded assemblies before throwing a
    ///     exception
    /// </summary>
    private const int MAX_UNLOAD_TRIES = 10;

    private static readonly Dictionary<string, HashSet<ModInfo>> _modInfos = new();

    /// <summary>
    ///     A reference to the AssemblyLoadContext used for mod loading
    ///     only a weak reference is used to ensure that the garbage collector is able to unload unneeded assemblies
    ///     This is crucial, as otherwise if the same mod is loaded multiple times (different versions) for example for
    ///     indexing the available mods
    ///     It would result in a exception if multiple versions of the mod assembly gets loaded
    /// </summary>
    private static WeakReference? _modLoadContext;

    private static readonly Dictionary<ushort, IMod> _loadedMods = new();

    private static WeakReference? _rootLoadContext;
    private static readonly HashSet<ushort> _loadedRootMods = new();

    /// <summary>
    ///     Get all available mod infos
    /// </summary>
    /// <returns>Enumerable containing all mod infos</returns>
    public static IEnumerable<ModInfo> GetAvailableMods()
    {
        return from modInfos in _modInfos
            from modInfo in modInfos.Value
            select modInfo;
    }

    /// <summary>
    ///     Load the specified mods
    /// </summary>
    public static void LoadGameMods(IEnumerable<ModInfo> mods)
    {
        RegistryManager.RegistryPhase = RegistryPhase.Mods;
        AssemblyLoadContext? modLoadContext = null;

        if (_modLoadContext is {IsAlive: true})
            modLoadContext = _modLoadContext.Target as AssemblyLoadContext;

        if (modLoadContext is null)
        {
            modLoadContext = new AssemblyLoadContext("ModLoadContext", true);
            _modLoadContext = new WeakReference(modLoadContext);
        }

        foreach (var modInfo in mods)
        {
            //Root mods only get loaded at the application startup
            if (modInfo.IsRootMod) continue;
            IMod? mod;
            ushort modId;
            if (modInfo.ModFileLocation.Length != 0)
            {
                //Load the assembly containing the mod
                var modFile = new FileInfo(modInfo.ModFileLocation);
                var modAssembly = modLoadContext.LoadFromAssemblyPath(modFile.FullName);

                //Find the type of the main mod class
                var modType = modAssembly.ExportedTypes.First(type =>
                    type.GetInterfaces().Any(i => i.GUID.Equals(typeof(IMod).GUID)));

                //instantiate the mod and check if its valid
                mod = Activator.CreateInstance(modType) as IMod;
                if (mod is null)
                {
                    Logger.WriteLog($"Mod {modInfo.ModName} ({modInfo.ModFileLocation}) could not be loaded",
                        LogImportance.Warning, "Modding");
                    continue;
                }

                var modDirectory = modFile.Directory?.FullName;
                Logger.AssertAndThrow(modDirectory is not null, "Mod directory not found... strange",
                    "Modding");

                //Register the mod and acquire a unique mod id
                modId = RegistryManager.RegisterModId(mod.StringIdentifier, modDirectory);
            }
            else // The only mod where the file location can be empty is the MintyCoreMod
            {
                mod = new MintyCoreMod();
                modId = RegistryManager.RegisterModId(mod.StringIdentifier, string.Empty);
            }

            mod.ModId = modId;
            _loadedMods.Add(modId, mod);
        }

        //Process the registry to load the content of the mods
        ProcessRegistry(false, LoadPhase.Pre | LoadPhase.Main | LoadPhase.Post);
    }

    /// <summary>
    ///     Load the <see cref="MintyCoreMod" /> and all registered root mods
    /// </summary>
    public static void LoadRootMods()
    {
        RegistryManager.RegistryPhase = RegistryPhase.Mods;


        MintyCoreMod mintyCoreMod = new();
        var mintyCoreModId = RegistryManager.RegisterModId(mintyCoreMod.StringIdentifier, string.Empty);
        mintyCoreMod.ModId = mintyCoreModId;
        _loadedMods.Add(mintyCoreModId, mintyCoreMod);
        _loadedRootMods.Add(mintyCoreModId);

        AssemblyLoadContext? rootLoadContext = null;

        if (_rootLoadContext is {IsAlive: true})
            rootLoadContext = _rootLoadContext.Target as AssemblyLoadContext;

        if (rootLoadContext is null)
        {
            rootLoadContext = new AssemblyLoadContext("ModLoadContext", true);
            _rootLoadContext = new WeakReference(rootLoadContext);
        }

        ModInfo additionalRootModInfo = default;
        foreach (var (_, modInfos) in _modInfos)
        {
            foreach (var modInfo in modInfos.Where(modInfo =>
                         modInfo.IsRootMod && !modInfo.ModId.Equals(mintyCoreMod.StringIdentifier)))
            {
                additionalRootModInfo = modInfo;
                break;
            }

            if (!string.IsNullOrEmpty(additionalRootModInfo.ModId)) break;
        }

        if (!string.IsNullOrEmpty(additionalRootModInfo.ModId))
        {
            var modFile = new FileInfo(additionalRootModInfo.ModFileLocation!);
            var modAssembly = rootLoadContext.LoadFromAssemblyPath(modFile.FullName);

            var modType = modAssembly.ExportedTypes.First(type =>
                type.GetInterfaces().Any(i => i.GUID.Equals(typeof(IMod).GUID)));

            if (Activator.CreateInstance(modType) is IMod mod)
            {
                var modDirectory = modFile.Directory?.FullName;
                Logger.AssertAndThrow(modDirectory is not null, "Mod directory not found... strange", "ECS");
                var modId = RegistryManager.RegisterModId(mod.StringIdentifier, modDirectory);
                mod.ModId = modId;
                _loadedMods.Add(modId, mod);
                _loadedRootMods.Add(modId);
            }
        }
    }

    /// <summary>
    ///     Get the instance of a loaded mod
    /// </summary>
    /// <param name="modId">Id of the mod</param>
    /// <returns>Instance of the mod</returns>
    public static IMod GetLoadedMod(ushort modId)
    {
        return _loadedMods[modId];
    }

    /// <summary>
    ///     Check whether or not a mod is a root mod
    /// </summary>
    /// <param name="modId">id of the mod to check</param>
    /// <returns>True if its a root mod; false if not or the mod is not present</returns>
    public static bool IsRootMod(ushort modId)
    {
        return _loadedRootMods.Contains(modId);
    }

    internal static void ProcessRegistry(bool loadRootMods, LoadPhase loadPhase)
    {
        if(loadPhase.HasFlag(LoadPhase.Pre))
        {
            foreach (var (id, mod) in _loadedMods)
            {
                if (_loadedRootMods.Contains(id) && !loadRootMods) continue;
                mod.PreLoad();
            }
        }

        if(loadPhase.HasFlag(LoadPhase.Main))
        {
            RegistryManager.RegistryPhase = RegistryPhase.Categories;
            foreach (var (id, mod) in _loadedMods)
            {
                if (_loadedRootMods.Contains(id) && !loadRootMods) continue;
                mod.Load();
            }

            RegistryManager.RegistryPhase = RegistryPhase.Objects;
            RegistryManager.ProcessRegistries();
            RegistryManager.RegistryPhase = RegistryPhase.None;
        }

        if(loadPhase.HasFlag(LoadPhase.Post))
        {
            foreach (var (id, mod) in _loadedMods)
            {
                if (_loadedRootMods.Contains(id) && !loadRootMods) continue;
                mod.PostLoad();
            }
        }
    }

    /// <summary>
    ///     Get an enumerable with all loaded mods including modId and mod instance
    /// </summary>
    public static IEnumerable<(string modId, ModVersion modVersion, IMod mod)> GetLoadedMods()
    {
        return from loadedMod in _loadedMods
            select (loadedMod.Value.StringIdentifier, loadedMod.Value.ModVersion, loadedMod.Value);
    }

    /// <summary>
    ///     Unload mods
    /// </summary>
    /// <param name="unloadRootMods">
    ///     Whether or not root mods will be unloaded. If false they get unloaded and reloaded
    ///     immediately
    /// </param>
    public static void UnloadMods(bool unloadRootMods)
    {
        var modsToRemove = !unloadRootMods
            ? _loadedMods.Where(x => !_loadedRootMods.Contains(x.Key)).Select(x => x.Key).ToArray()
            : _loadedMods.Keys.ToArray();

        RegistryManager.Clear(modsToRemove);

        FreeMods(modsToRemove);

        //At this point no reference to any type of the mods to unload should remain in the engine and root mods (if any present)
        //To ensure proper unloading of the mod assemblies
        if (_modLoadContext is null) return;
        WaitForUnloading(_modLoadContext);

        if (unloadRootMods && _rootLoadContext is not null) WaitForUnloading(_rootLoadContext);
    }

    // ReSharper disable once ParameterTypeCanBeEnumerable.Local; Change to IEnumerable<ushort> slows down the foreach loop
    private static void FreeMods(ushort[] modsToUnload)
    {
        //Removes the mod reference and call unload
        foreach (var id in modsToUnload)
        {
            if (Logger.AssertAndLog(_loadedMods.Remove(id, out var mod),
                    $"Failed to remove and unload mod with numeric id {id}", "Modding", LogImportance.Warning))
                mod?.Unload();

            _loadedRootMods.Remove(id);
        }
    }

    internal static void SearchMods(IEnumerable<DirectoryInfo>? additionalModDirectories = null)
    {
        IEnumerable<DirectoryInfo> modDirs = Array.Empty<DirectoryInfo>();
        var modFolder = new DirectoryInfo($"{Directory.GetCurrentDirectory()}/mods");

        if (modFolder.Exists)
            modDirs = modDirs.Concat(modFolder.EnumerateDirectories("*", SearchOption.TopDirectoryOnly));
        if (additionalModDirectories is not null) modDirs = modDirs.Concat(additionalModDirectories);

        {
            IMod mod = new MintyCoreMod();

            ModInfo modInfo = new(string.Empty, mod.StringIdentifier, mod.ModName, mod.ModDescription,
                mod.ModVersion, mod.ModDependencies, mod.ExecutionSide, true);

            if (!_modInfos.ContainsKey(modInfo.ModId)) _modInfos.Add(modInfo.ModId, new HashSet<ModInfo>());
            _modInfos[modInfo.ModId].Add(modInfo);
        }

        var sw = Stopwatch.StartNew();
        foreach (var dllFile in
                 from modDir in modDirs
                 from dllFile in modDir.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly)
                 select dllFile)
        {
            if (IsModFile(dllFile, out var loadReference, out var modInfo))
            {
                if (!_modInfos.ContainsKey(modInfo.ModId)) _modInfos.Add(modInfo.ModId, new HashSet<ModInfo>());
                _modInfos[modInfo.ModId].Add(modInfo);
            }

            WaitForUnloading(loadReference);
        }

        sw.Stop();
        Logger.WriteLog($"Mod indexing took {sw.Elapsed}", LogImportance.Info, "ModManager");
    }

    private static bool IsModFile(FileInfo dllFile, out WeakReference weakReference, out ModInfo modInfo)
    {
        var modLoadContext = new AssemblyLoadContext("CheckIsMod", true);
        weakReference = new WeakReference(modLoadContext);
        modInfo = default;

        Assembly assembly;
        Type? modType = null;

        try
        {
            assembly = modLoadContext.LoadFromAssemblyPath(dllFile.FullName);
        }
        catch (Exception)
        {
            modLoadContext.Unload();
            return false;
        }

        foreach (var exportedType in assembly.ExportedTypes)
            if (exportedType.GetInterfaces().Any(x => x.GUID.Equals(typeof(IMod).GUID)))
                modType = exportedType;

        if (modType is null)
        {
            modLoadContext.Unload();
            return false;
        }

        if (Activator.CreateInstance(modType) is not IMod mod)
        {
            modLoadContext.Unload();
            return false;
        }

        var isRootMod =
            modType.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(RootModAttribute));

        modInfo = new ModInfo(dllFile.FullName, mod.StringIdentifier, mod.ModName, mod.ModDescription,
            mod.ModVersion, mod.ModDependencies, mod.ExecutionSide, isRootMod);

        modLoadContext.Unload();
        return true;
    }

    private static void WaitForUnloading(WeakReference loadContextReference)
    {
        //While the AssemblyLoadContext is alive (the object was not collected by the garbage collector yet)
        //the mod assemblies are still loaded. When the AssemblyLoadContext gets collected all loaded mod assemblies got collected too
        for (var i = 0; i < MAX_UNLOAD_TRIES && loadContextReference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        if (!loadContextReference.IsAlive) return;

        Logger.WriteLog("Failed to unload assemblies", LogImportance.Warning, "Modding");
    }

    /// <summary>
    ///     Check if the given set of mods is compatible to the loaded mods
    /// </summary>
    /// <param name="infoAvailableMods"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool ModsCompatible(IEnumerable<(string modId, ModVersion version)> infoAvailableMods)
    {
        return _loadedMods.Values.All(mod => infoAvailableMods.Any(availableMod =>
            mod.StringIdentifier.Equals(availableMod.modId) && mod.ModVersion.Compatible(availableMod.version)));
    }
}

[Flags]
enum LoadPhase
{
    None = 0,
    Pre = 1,
    Main = 2,
    Post = 4
}