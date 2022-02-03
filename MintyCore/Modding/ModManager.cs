using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
/// Class which handles mod indexing, loading and unloading.
/// Also additional mod related functions
/// </summary>
public static class ModManager
{
    /// <summary>
    /// Event which get fired after a mod reset (game mods unloaded and root mods reloaded)
    /// </summary>
    public static event Action AfterModReset = delegate { };

    private static readonly Dictionary<string, HashSet<ModInfo>> _modInfos = new();

    private static WeakReference? _modLoadContext;
    private static readonly Dictionary<ushort, IMod> _loadedMods = new();

    private static WeakReference? _rootLoadContext;
    private static readonly HashSet<ushort> _loadedRootMods = new();


    private static readonly int maxUnloadTries = 10;

    /// <summary>
    /// Get all available mod infos
    /// </summary>
    /// <returns>Enumerable containing all mod infos</returns>
    public static IEnumerable<ModInfo> GetAvailableMods()
    {
        return from modInfos in _modInfos
            from modInfo in modInfos.Value
            select modInfo;
    }
    
    /// <summary>
    /// Load the specified mods
    /// </summary>
    public static void LoadGameMods(IEnumerable<ModInfo> mods)
    {
        RegistryManager.RegistryPhase = RegistryPhase.MODS;
        AssemblyLoadContext? modLoadContext = null;

        if (_modLoadContext != null && _modLoadContext.IsAlive)
            modLoadContext = _modLoadContext.Target as AssemblyLoadContext;

        if (modLoadContext is null)
        {
            modLoadContext = new AssemblyLoadContext("ModLoadContext", true);
            _modLoadContext = new WeakReference(modLoadContext);
        }

        foreach (var modInfo in mods)
        {
            if (modInfo.IsRootMod) continue;
            IMod mod;
            ushort modId;
            if (modInfo.ModFileLocation.Length != 0)
            {
                var modFile = new FileInfo(modInfo.ModFileLocation);
                var modAssembly = modLoadContext.LoadFromAssemblyPath(modFile.FullName);

                var modType = modAssembly.ExportedTypes.First(type =>
                    type.GetInterfaces().Any(i => i.GUID.Equals(typeof(IMod).GUID)));
                mod = Activator.CreateInstance(modType) as IMod;
                if (mod is null) continue;

                var modDirectory = modFile.Directory?.FullName ??
                                   throw new DirectoryNotFoundException("Mod directory not found... strange");

                modId = RegistryManager.RegisterModId(mod.StringIdentifier, modDirectory);
            }
            else
            {
                mod = new MintyCoreMod();
                modId = RegistryManager.RegisterModId(mod.StringIdentifier, string.Empty);
            }

            mod.ModId = modId;
            _loadedMods.Add(modId, mod);
        }

        ProcessRegistry(false);
    }

    /// <summary>
    /// Load the <see cref="MintyCoreMod"/> and all registered root mods
    /// </summary>
    public static void LoadRootMods()
    {
        RegistryManager.RegistryPhase = RegistryPhase.MODS;


        MintyCoreMod mintyCoreMod = new();
        var mintyCoreModId = RegistryManager.RegisterModId(mintyCoreMod.StringIdentifier, string.Empty);
        mintyCoreMod.ModId = mintyCoreModId;
        _loadedMods.Add(mintyCoreModId, mintyCoreMod);
        _loadedRootMods.Add(mintyCoreModId);

        AssemblyLoadContext? rootLoadContext = null;

        if (_rootLoadContext != null && _rootLoadContext.IsAlive)
            rootLoadContext = _rootLoadContext.Target as AssemblyLoadContext;

        if (rootLoadContext is null)
        {
            rootLoadContext = new AssemblyLoadContext("ModLoadContext", true);
            _rootLoadContext = new WeakReference(rootLoadContext);
        }

        ModInfo additionalRootModInfo = default;
        foreach (var (_, modInfos) in _modInfos)
        {
            foreach (var modInfo in modInfos)
            {
                if (!modInfo.IsRootMod || modInfo.ModId.Equals(mintyCoreMod.StringIdentifier)) continue;
                additionalRootModInfo = modInfo;
                break;
            }

            if (!String.IsNullOrEmpty(additionalRootModInfo.ModId))
            {
                break;
            }
        }

        if (!String.IsNullOrEmpty(additionalRootModInfo.ModId))
        {
            var modFile = new FileInfo(additionalRootModInfo.ModFileLocation);
            var modAssembly = rootLoadContext.LoadFromAssemblyPath(modFile.FullName);

            var modType = modAssembly.ExportedTypes.First(type =>
                type.GetInterfaces().Any(i => i.GUID.Equals(typeof(IMod).GUID)));

            if (Activator.CreateInstance(modType) is IMod mod)
            {
                var modDirectory = modFile.Directory?.FullName ??
                                   throw new DirectoryNotFoundException("Mod directory not found... strange");
                var modId = RegistryManager.RegisterModId(mod.StringIdentifier, modDirectory);
                mod.ModId = modId;
                _loadedMods.Add(modId, mod);
                _loadedRootMods.Add(modId);
            }
        }

        ProcessRegistry(true);
    }

    private static void ProcessRegistry(bool loadRootMods)
    {
        RegistryManager.RegistryPhase = RegistryPhase.CATEGORIES;

        foreach (var (id, mod) in _loadedMods)
        {
            if (_loadedRootMods.Contains(id) && !loadRootMods) continue;
            mod.PreLoad();
        }

        foreach (var (id, mod) in _loadedMods)
        {
            if (_loadedRootMods.Contains(id) && !loadRootMods) continue;
            mod.Load();
        }

        foreach (var (id, mod) in _loadedMods)
        {
            if (_loadedRootMods.Contains(id) && !loadRootMods) continue;
            mod.PostLoad();
        }

        RegistryManager.RegistryPhase = RegistryPhase.OBJECTS;
        RegistryManager.ProcessRegistries();
        RegistryManager.RegistryPhase = RegistryPhase.NONE;
    }

    /// <summary>
    /// Get an enumerable with all loaded mods including modId and mod instance
    /// </summary>
    public static IEnumerable<(string modId, ModVersion modVersion, IMod mod)> GetLoadedMods()
    {
        return from loadedMod in _loadedMods
            select (loadedMod.Value.StringIdentifier, loadedMod.Value.ModVersion, loadedMod.Value);
    }

    /// <summary>
    /// Unload mods
    /// </summary>
    /// <param name="unloadRootMods">Whether or not root mods will be unloaded. If false they get unloaded and reloaded immediately</param>
    public static void UnloadMods(bool unloadRootMods)
    {
        var modsToRemove = FreeMods(unloadRootMods);
        RegistryManager.Clear(modsToRemove);

        if (_modLoadContext is null) return;
        WaitForUnloading(_modLoadContext);

        if (unloadRootMods) return;
        ProcessRegistry(true);
        AfterModReset();
    }

    private static HashSet<ushort> FreeMods(bool unloadRootMods)
    {
        HashSet<ushort> remove = new();
        foreach (var (id, mod) in _loadedMods)
        {
            if (_loadedRootMods.Contains(id) && !unloadRootMods) continue;
            mod.Unload();
            remove.Add(id);
        }

        foreach (var id in remove)
        {
            _loadedMods.Remove(id);
        }

        return remove;
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

            ModInfo modInfo = new( string.Empty, mod.StringIdentifier, mod.ModName, mod.ModDescription,
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
        Logger.WriteLog($"Mod indexing took {sw.Elapsed}", LogImportance.INFO, "ModManager");
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

        var mod = Activator.CreateInstance(modType) as IMod;
        if (mod is null)
        {
            modLoadContext.Unload();
            return false;
        }

        bool isRootMod =
            modType.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(RootModAttribute));

        modInfo = new ModInfo(dllFile.FullName, mod.StringIdentifier, mod.ModName, mod.ModDescription,
            mod.ModVersion, mod.ModDependencies, mod.ExecutionSide, isRootMod);

        modLoadContext.Unload();
        return true;
    }

    private static void WaitForUnloading(WeakReference loadContextReference)
    {
        for (var i = 0; i < maxUnloadTries && loadContextReference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        if (!loadContextReference.IsAlive) return;

        Logger.WriteLog("Failed to unload assemblies", LogImportance.WARNING, "Modding");
    }

    /// <summary>
    /// Check if the given set of mods is compatible to the loaded mods
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