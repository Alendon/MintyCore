using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
///     Class which handles mod indexing, loading and unloading.
///     Also additional mod related functions
/// </summary>
[PublicAPI]
public static class ModManager
{
    /// <summary>
    ///     The maximum tries to trigger and wait for the garbage collector to collect unneeded assemblies before throwing a
    ///     exception
    /// </summary>
    private const int MaxUnloadTries = 10;

    private static readonly Dictionary<string, List<ModManifest>> _modManifests = new();

    /// <summary>
    ///     A reference to the AssemblyLoadContext used for mod loading
    ///     only a weak reference is used to ensure that the garbage collector is able to unload unneeded assemblies
    ///     This is crucial, as otherwise if the same mod is loaded multiple times (different versions) for example for
    ///     indexing the available mods
    ///     It would result in a exception if multiple versions of the mod assembly gets loaded
    /// </summary>
    private static GCHandle _modLoadContext;

    private static readonly Dictionary<ushort, IMod> _loadedMods = new();
    private static readonly Dictionary<ushort, ZipArchive> _loadedModArchives = new();
    private static readonly Dictionary<ushort, ModManifest> _loadedModManifests = new();
    private static readonly Dictionary<ushort, List<ExternalDependency>> _loadedModsExternalDependencies = new();

    private static GCHandle _rootLoadContext;
    private static readonly HashSet<ushort> _loadedRootMods = new();

    /// <summary>
    ///     Get all available mod infos
    /// </summary>
    /// <returns>Enumerable containing all mod infos</returns>
    public static IEnumerable<ModManifest> GetAvailableMods(bool latestVersionsOnly)
    {
        var manifests = new List<ModManifest>();

        foreach (var entry in _modManifests)
        {
            if (!latestVersionsOnly)
            {
                manifests.AddRange(from infos in entry.Value select infos);
                continue;
            }

            var latestVersion = entry.Value.MaxBy(info => info.Version);
            if (latestVersion is not null)
                manifests.Add(latestVersion);
        }

        return manifests;
    }

    /// <summary>
    ///     Load the specified mods
    /// </summary>
    public static void LoadGameMods(IEnumerable<ModManifest> mods)
    {
        RegistryManager.RegistryPhase = RegistryPhase.Mods;
        SharedAssemblyLoadContext? modLoadContext = null;

        if (_modLoadContext is {IsAllocated: true})
            modLoadContext = _modLoadContext.Target as SharedAssemblyLoadContext;

        if (modLoadContext is null)
        {
            modLoadContext = new SharedAssemblyLoadContext();
            _modLoadContext = GCHandle.Alloc(modLoadContext, GCHandleType.Normal);
        }

        foreach (var modInfo in mods)
        {
            //Root mods only get loaded at the application startup
            if (modInfo.IsRootMod)
            {
                Logger.AssertAndThrow(
                    _loadedModManifests.Any(manifest => manifest.Value.Identifier == modInfo.Identifier),
                    $"Mod {modInfo.Identifier} is marked as root mod, but is not loaded", "ModManager");
                continue;
            }

            Logger.AssertAndThrow(modInfo.ModFile is not null, $"Mod {modInfo.Identifier} has no mod file",
                "ModManager");

            var modArchive = ZipFile.OpenRead(modInfo.ModFile.FullName);

            foreach (var externalDependency in modInfo.ExternalDependencies)
            {
                var dependencyEntry =
                    modArchive.Entries.FirstOrDefault(x => x.FullName.EndsWith($"/{externalDependency.DllName}"));

                Logger.AssertAndThrow(dependencyEntry is not null,
                    $"Mod {modInfo.Identifier} has an external dependency {externalDependency.DllName} which is not included in the mod archive",
                    "ModManager");

                LoadDll(modLoadContext, dependencyEntry);
            }

            var modDllEntry = modArchive.Entries.First(x => x.Name == x.FullName && x.Name.EndsWith(".dll"));
            using var modDllStream = modDllEntry.Open();

            using var modDllMemoryStream = new MemoryStream();
            modDllStream.CopyTo(modDllMemoryStream);
            modDllMemoryStream.Seek(0, SeekOrigin.Begin);

            var modAssembly = modLoadContext.CustomLoadFromStream(modDllMemoryStream);

            var modType = modAssembly.ExportedTypes.FirstOrDefault(type =>
                type.GetInterfaces().Any(i => i.GUID.Equals(typeof(IMod).GUID)));

            Logger.AssertAndThrow(modType is not null, $"Mod main class in dll {modInfo.ModFile} not found",
                "Modding");

            var mod = Activator.CreateInstance(modType) as IMod;

            Logger.AssertAndThrow(mod is not null,
                $"Mod main class in dll {modInfo.ModFile} could not be instantiated",
                "Modding");

            var modId = RegistryManager.RegisterModId(modInfo.Identifier);
            mod.ModId = modId;

            _loadedMods.Add(modId, mod);
            _loadedModArchives.Add(modId, modArchive);
            _loadedModManifests.Add(modId, modInfo);
            _loadedModsExternalDependencies.Add(modId, modInfo.ExternalDependencies);
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

        var mintyCoreManifest = MintyCoreMod.ConstructManifest();
        var mintyCoreModId = RegistryManager.RegisterModId(mintyCoreManifest.Identifier);
        mintyCoreMod.ModId = mintyCoreModId;
        _loadedMods.Add(mintyCoreModId, mintyCoreMod);
        _loadedModManifests.Add(mintyCoreModId, mintyCoreManifest);
        _loadedRootMods.Add(mintyCoreModId);

        SharedAssemblyLoadContext? rootLoadContext = null;

        if (_rootLoadContext is {IsAllocated: true})
            rootLoadContext = _rootLoadContext.Target as SharedAssemblyLoadContext;

        if (rootLoadContext is null)
        {
            rootLoadContext = new SharedAssemblyLoadContext();
            _rootLoadContext = GCHandle.Alloc(rootLoadContext, GCHandleType.Normal);
        }

        foreach (var modInfo in _modManifests.Values)
        {
            var latestMod = modInfo.MaxBy(x => x.Version);

            if (latestMod is null) continue;

            if (latestMod.IsRootMod is false) continue;

            if (latestMod.ModFile is null) continue;

            var modArchive = ZipFile.OpenRead(latestMod.ModFile.FullName);

            foreach (var externalDependency in latestMod.ExternalDependencies)
            {
                var dependencyEntry = modArchive.Entries.First(x => x.Name.EndsWith($"/{externalDependency.DllName}"));
                LoadDll(rootLoadContext, dependencyEntry);
            }

            var modDllEntry = modArchive.Entries.First(x => x.Name == x.FullName && x.Name.EndsWith(".dll"));
            using var modDllStream = modDllEntry.Open();
            using var memoryStream = new MemoryStream();
            modDllStream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var modAssembly = rootLoadContext.CustomLoadFromStream(memoryStream);


            var modType = modAssembly.ExportedTypes.FirstOrDefault(type =>
                type.GetInterfaces().Any(i => i.GUID.Equals(typeof(IMod).GUID)));

            Logger.AssertAndThrow(modType is not null, $"Mod main class in dll {latestMod.ModFile} not found",
                "Modding");

            var mod = Activator.CreateInstance(modType) as IMod;

            Logger.AssertAndThrow(mod is not null,
                $"Mod main class in dll {latestMod.ModFile} could not be instantiated",
                "Modding");

            var modId = RegistryManager.RegisterModId(latestMod.Identifier);
            mod.ModId = modId;

            _loadedRootMods.Add(modId);
            _loadedMods.Add(modId, mod);
            _loadedModArchives.Add(modId, modArchive);
            _loadedModManifests.Add(modId, latestMod);
            _loadedModsExternalDependencies.Add(modId, latestMod.ExternalDependencies);
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
        if (loadPhase.HasFlag(LoadPhase.Pre))
        {
            foreach (var (id, mod) in _loadedMods)
            {
                if (_loadedRootMods.Contains(id) && !loadRootMods) continue;
                mod.PreLoad();
            }
        }

        if (loadPhase.HasFlag(LoadPhase.Main))
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

        if (!loadPhase.HasFlag(LoadPhase.Post)) return;

        foreach (var (id, mod) in _loadedMods)
        {
            if (_loadedRootMods.Contains(id) && !loadRootMods) continue;
            mod.PostLoad();
        }
    }

    /// <summary>
    ///     Get an enumerable with all loaded mods including modId and mod instance
    /// </summary>
    public static IEnumerable<(string modId, Version modVersion, IMod mod)> GetLoadedMods()
    {
        return _loadedMods.Select(modEntry =>
        {
            var manifest = _loadedModManifests[modEntry.Key];
            return (manifest.Identifier, manifest.Version, modEntry.Value);
        });
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
        if (_modLoadContext is { IsAllocated: false }) return;
        WaitForUnloading(_modLoadContext);

        if (unloadRootMods && _rootLoadContext is {IsAllocated: true}) WaitForUnloading(_rootLoadContext);
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

            if (_loadedModArchives.Remove(id, out var archive)) archive.Dispose();

            _loadedModManifests.Remove(id);
            _loadedModsExternalDependencies.Remove(id);
            _loadedRootMods.Remove(id);
        }
    }

    internal static void SearchMods(IEnumerable<DirectoryInfo>? additionalModDirectories = null)
    {
        var modFolders = new List<DirectoryInfo>
        {
            new(Path.Combine(Environment.CurrentDirectory, "mods"))
        };
        if (!modFolders[0].Exists) modFolders[0].Create();

        modFolders.AddRange(additionalModDirectories ?? Array.Empty<DirectoryInfo>());


        var modFiles = modFolders.SelectMany(
            x => x.GetFiles("*.mcmod", SearchOption.TopDirectoryOnly));

        var coreManifest = MintyCoreMod.ConstructManifest();

        if (!_modManifests.ContainsKey(coreManifest.Identifier))
            _modManifests.Add(coreManifest.Identifier, new());

        _modManifests[coreManifest.Identifier].Add(coreManifest);

        foreach (var modFile in modFiles)
        {
            using var fileStream = modFile.OpenRead();
            using var modArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);

            if (modArchive.Entries.Count(x =>
                    //This ensures that the file is in the root of the archive
                    x.Name == x.FullName &&
                    x.Name.EndsWith(".dll")) != 1)
            {
                Logger.WriteLog($"Invalid mod archive (multiple dlls or no dlls) {modFile}", LogImportance.Warning,
                    "ModManager");
                continue;
            }

            var manifestEntry = modArchive.GetEntry("manifest.json");
            if (manifestEntry is null)
            {
                Logger.WriteLog($"Invalid mod archive (no manifest.json) {modFile}", LogImportance.Warning,
                    "ModManager");
                continue;
            }

            using var manifestStream = manifestEntry.Open();
            var manifest = JsonSerializer.Deserialize<ModManifest>(manifestStream);

            if (manifest is null)
            {
                Logger.WriteLog($"Invalid mod archive (invalid manifest.json) {modFile}", LogImportance.Warning,
                    "ModManager");
                continue;
            }

            manifest.ModFile = modFile;

            if (!_modManifests.ContainsKey(manifest.Identifier))
            {
                _modManifests.Add(manifest.Identifier, new());
            }

            _modManifests[manifest.Identifier].Add(manifest);
        }
    }

    private static void WaitForUnloading(GCHandle loadContextReference)
    {
        var reference = new WeakReference(loadContextReference.Target);
        
        loadContextReference.Free();

        for (var i = 0; i < MaxUnloadTries && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        if (!reference.IsAlive) return;

        Logger.WriteLog("Failed to unload assemblies", LogImportance.Warning, "Modding");
    }

    /// <summary>
    ///     Check if the given set of mods is compatible to the loaded mods
    /// </summary>
    /// <param name="infoAvailableMods"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool ModsCompatible(IEnumerable<(string modId, Version version)> infoAvailableMods)
    {
        return _loadedModManifests.Values.All(
            loadedManifest => infoAvailableMods.Any(
                available => available.modId == loadedManifest.Identifier
                             && available.version.CompatibleWith(loadedManifest.Version)));
    }

    /// <summary>
    /// Get a stream to the requested resource file
    /// </summary>
    /// <param name="resource">Id associated with the resource file</param>
    /// <remarks>Do not forget do dispose the stream</remarks>
    /// <returns>Stream containing the information of the resource file</returns>
    public static Stream GetResourceFileStream(Identification resource)
    {
        var archive = _loadedModArchives[resource.Mod];
        var entry = archive.GetEntry(RegistryManager.GetResourceFileName(resource));
        Logger.AssertAndThrow(entry is not null, $"Requested resource file {resource} does not exist", "ModManager");

        using var zipStream = entry.Open();

        var memoryStream = new MemoryStream();
        zipStream.CopyTo(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }

    public static bool FileExists(ushort modId, string location)
    {
        if (!_loadedModArchives.TryGetValue(modId, out var archive)) return false;

        return archive.GetEntry(location) is not null;
    }

    private static void LoadDll(SharedAssemblyLoadContext loadContext, ZipArchiveEntry entry)
    {
        using var zipStream = entry.Open();
        using var memoryStream = new MemoryStream();
        zipStream.CopyTo(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        loadContext.CustomLoadFromStream(memoryStream);
    }
}

[Flags]
internal enum LoadPhase
{
    None = 0,
    Pre = 1,
    Main = 2,
    Post = 4
}