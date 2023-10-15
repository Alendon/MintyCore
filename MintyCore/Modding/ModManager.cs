using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using Autofac;
using Autofac.Core.Lifetime;
using Autofac.Features.Metadata;
using JetBrains.Annotations;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
///     Class which handles mod indexing, loading and unloading.
///     Also additional mod related functions
/// </summary>
[PublicAPI]
[Singleton<IModManager>]
public class ModManager : IModManager
{
    /// <summary>
    ///     The maximum tries to trigger and wait for the garbage collector to collect unneeded assemblies before throwing a
    ///     exception
    /// </summary>
    private const int MaxUnloadTries = 10;

    private readonly Dictionary<string, List<ModManifest>> _modManifests = new();

    /// <summary>
    ///     A reference to the AssemblyLoadContext used for mod loading
    ///     only a weak reference is used to ensure that the garbage collector is able to unload unneeded assemblies
    ///     This is crucial, as otherwise if the same mod is loaded multiple times (different versions) for example for
    ///     indexing the available mods
    ///     It would result in a exception if multiple versions of the mod assembly gets loaded
    /// </summary>
    private GCHandle _modLoadContext;

    private readonly Dictionary<ushort, IMod> _loadedMods = new();
    private readonly Dictionary<ushort, ZipArchive> _loadedModArchives = new();
    private readonly Dictionary<ushort, ModManifest> _loadedModManifests = new();

    private GCHandle _rootLoadContext;
    private readonly HashSet<ushort> _loadedRootMods = new();

    private ILifetimeScope? _rootLifetimeScope;
    private ILifetimeScope? _modLifetimeScope;
    private readonly ILifetimeScope _engineLifetimeScope;

    /// <inheritdoc />
    public ILifetimeScope ModLifetimeScope => _modLifetimeScope ??
                                              _rootLifetimeScope ??
                                              throw new MintyCoreException("Mod lifetime scope not available");

    public ModManager(ILifetimeScope lifetimeScope)
    {
        _engineLifetimeScope = lifetimeScope;
        RegistryManager = new RegistryManager(this);
    }

    /// <inheritdoc />
    public IRegistryManager RegistryManager { get; }

    /// <summary>
    ///     Get all available mod infos
    /// </summary>
    /// <returns>Enumerable containing all mod infos</returns>
    public IEnumerable<ModManifest> GetAvailableMods(bool latestVersionsOnly)
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
    public void LoadGameMods(IEnumerable<ModManifest> mods)
    {
        RegistryManager.RegistryPhase = RegistryPhase.Mods;
        SharedAssemblyLoadContext? modLoadContext = null;

        if (_modLoadContext is { IsAllocated: true })
            modLoadContext = _modLoadContext.Target as SharedAssemblyLoadContext;

        if (modLoadContext is null)
        {
            modLoadContext = new SharedAssemblyLoadContext();
            _modLoadContext = GCHandle.Alloc(modLoadContext, GCHandleType.Normal);
        }

        var modIds = new Dictionary<string, ushort>();
        Action<ContainerBuilder> containerBuilder = _ => { };

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

            Assembly assembly = LoadModAssembly(modArchive, modLoadContext);

            LoadExternalDependencies(modInfo, modArchive, modLoadContext);

            var modType = FindModType(assembly);

            Logger.AssertAndThrow(modType is not null, $"Mod main class in dll {modInfo.ModFile} not found",
                "Modding");

            containerBuilder += LoadMod(modInfo, modIds, modArchive, modType, false);
            containerBuilder += LoadSingletons(assembly);
            containerBuilder += LoadRegistryProvider(assembly, modInfo.Identifier);
            containerBuilder += LoadObjectRegistryProviders(assembly, false, modInfo.Identifier);
            containerBuilder += LoadByCustomProvider(assembly);
        }

        Logger.AssertAndThrow(_rootLifetimeScope is not null, "Root lifetime scope not available", "ModManager");
        _modLifetimeScope =
            _rootLifetimeScope.BeginLoadContextLifetimeScope("game_mods", modLoadContext, containerBuilder);

        foreach (var meta in _modLifetimeScope.Resolve<IEnumerable<Meta<IMod>>>())
        {
            Logger.AssertAndThrow(meta.Metadata.TryGetValue(ModTag.MetadataName, out var modTagObject) &&
                                  modTagObject is ModTag,
                $"Mod {meta.Value.GetType().FullName} has no mod tag", "Modding");

            //It is sadly not possible to write this directly in the assertion, as the compiler does not recognize that the variable will be assigned
            var modTag = (modTagObject as ModTag)!;
            if (modTag.IsRootMod) continue;

            var modId = modIds[modTag.Identifier];
            _loadedMods.Add(modId, meta.Value);
        }

        //Process the registry to load the content of the mods
        ProcessRegistry(false, LoadPhase.Pre | LoadPhase.Main | LoadPhase.Post);
    }

    /// <summary>
    ///     Load the <see cref="MintyCoreMod" /> and all registered root mods
    /// </summary>
    public void LoadRootMods()
    {
        RegistryManager.RegistryPhase = RegistryPhase.Mods;
        var containerBuilder = (ContainerBuilder _) => { };
        var modIds = new Dictionary<string, ushort>();


        containerBuilder += LoadMintyCoreMod(modIds);
        containerBuilder +=
            LoadRegistryProvider(typeof(MintyCoreMod).Assembly, MintyCoreMod.ConstructManifest().Identifier);
        containerBuilder += LoadObjectRegistryProviders(typeof(MintyCoreMod).Assembly, true,
            MintyCoreMod.ConstructManifest().Identifier);

        SharedAssemblyLoadContext? rootLoadContext = null;

        if (_rootLoadContext is { IsAllocated: true })
            rootLoadContext = _rootLoadContext.Target as SharedAssemblyLoadContext;

        if (rootLoadContext is null)
        {
            rootLoadContext = new SharedAssemblyLoadContext();
            _rootLoadContext = GCHandle.Alloc(rootLoadContext, GCHandleType.Normal);
        }

        var rootModsToLoad = _modManifests.Values.Select(mods => mods.MaxBy(m => m.Version))
            .Where(mod => mod is { IsRootMod: true, ModFile: not null }).Select(m => m!).ToArray();


        foreach (var manifest in rootModsToLoad)
        {
            var modArchive = ZipFile.OpenRead(manifest.ModFile!.FullName);

            LoadExternalDependencies(manifest, modArchive, rootLoadContext);

            var modAssembly = LoadModAssembly(modArchive, rootLoadContext);
            var modType = FindModType(modAssembly);

            Logger.AssertAndThrow(modType is not null, $"Mod main class in dll {manifest.ModFile} not found",
                "Modding");


            containerBuilder += LoadMod(manifest, modIds, modArchive, modType, true);
            containerBuilder += LoadSingletons(modAssembly);
            containerBuilder += LoadRegistryProvider(modAssembly, manifest.Identifier);
            containerBuilder += LoadObjectRegistryProviders(modAssembly, true, manifest.Identifier);
            containerBuilder += LoadByCustomProvider(modAssembly);
        }

        _rootLifetimeScope =
            _engineLifetimeScope.BeginLoadContextLifetimeScope("root_mods", rootLoadContext, containerBuilder);

        foreach (var meta in _rootLifetimeScope.Resolve<IEnumerable<Meta<IMod>>>())
        {
            Logger.AssertAndThrow(meta.Metadata.TryGetValue(ModTag.MetadataName, out var modTagObject) &&
                                  modTagObject is ModTag,
                $"Mod {meta.Value.GetType().FullName} has no mod tag", "Modding");

            //It is sadly not possible to write this directly in the assertion, as the compiler does not recognize that the variable will be assigned
            var modTag = (modTagObject as ModTag)!;

            var modId = modIds[modTag.Identifier];
            _loadedMods.Add(modId, meta.Value);
        }
    }

    private void LoadExternalDependencies(ModManifest manifest, ZipArchive modArchive,
        SharedAssemblyLoadContext rootLoadContext)
    {
        foreach (var externalDependency in manifest.ExternalDependencies)
        {
            var dependencyEntry = modArchive.Entries.First(x => x.Name.Contains($"{externalDependency.DllName}"));
            LoadDll(rootLoadContext, dependencyEntry);
        }
    }

    private static Assembly LoadModAssembly(ZipArchive modArchive, SharedAssemblyLoadContext rootLoadContext)
    {
        var modDllEntry = modArchive.Entries.First(x => x.Name == x.FullName && x.Name.EndsWith(".dll"));
        using var modDllStream = modDllEntry.Open();
        using var memoryStream = new MemoryStream();
        modDllStream.CopyTo(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return rootLoadContext.CustomLoadFromStream(memoryStream);
    }

    private static Type? FindModType(Assembly modAssembly)
    {
        var modType = modAssembly.ExportedTypes.FirstOrDefault(type =>
            Array.Exists(type.GetInterfaces(), i => i.GUID.Equals(typeof(IMod).GUID)));
        return modType;
    }

    private Action<ContainerBuilder> LoadSingletons(Assembly assembly) =>
        builder => builder.RegisterMarkedSingletons(assembly,
            Engine.HeadlessModeActive ? SingletonContextFlags.None : SingletonContextFlags.NoHeadless);

    private Action<ContainerBuilder> LoadMintyCoreMod(Dictionary<string, ushort> modIds)
    {
        var mintyCoreManifest = MintyCoreMod.ConstructManifest();

        var mintyCoreModId = RegistryManager.RegisterModId(mintyCoreManifest.Identifier);

        modIds.Add(mintyCoreManifest.Identifier, mintyCoreModId);
        _loadedModManifests.Add(mintyCoreModId, mintyCoreManifest);
        _loadedRootMods.Add(mintyCoreModId);

        return builder => builder.RegisterType<MintyCoreMod>().As<IMod>()
            .Named<MintyCoreMod>(AutofacHelper.UnsafeSelfName)
            .WithMetadata(ModTag.MetadataName, new ModTag(true, mintyCoreManifest.Identifier))
            .SingleInstance();
    }

    private Action<ContainerBuilder> LoadMod(ModManifest manifest, Dictionary<string, ushort> modIds,
        ZipArchive modArchive, Type modType, bool isRootMod)
    {
        var modId = RegistryManager.RegisterModId(manifest.Identifier);
        modIds.Add(manifest.Identifier, modId);
        _loadedRootMods.Add(modId);
        _loadedModArchives.Add(modId, modArchive);
        _loadedModManifests.Add(modId, manifest);

        Action<ContainerBuilder> bAction = builder =>
        {
            builder.RegisterType(modType).As<IMod>()
                .Named(AutofacHelper.UnsafeSelfName, modType)
                .WithMetadata(ModTag.MetadataName, new ModTag(isRootMod, manifest.Identifier))
                .SingleInstance();
        };
        return bAction;
    }

    private static Action<ContainerBuilder> LoadRegistryProvider(Assembly assembly, string modId)
    {
        var registryProviderType = assembly.ExportedTypes.FirstOrDefault(
            type =>
            {
                var providerAttribute = type.GetCustomAttribute<RegistryProviderAttribute>();
                var interfaces = type.GetInterfaces();
                return providerAttribute is not null &&
                       Array.Exists(interfaces, t => t.GUID.Equals(typeof(IRegistryProvider).GUID));
            });

        if (registryProviderType is null) return _ => { };
        return builder => builder.RegisterType(registryProviderType)
            .Keyed<IRegistryProvider>(modId);
    }

    private static Action<ContainerBuilder> LoadObjectRegistryProviders(Assembly assembly, bool isRootMod, string modId)
    {
        var providerTypesAndAttributes = assembly.ExportedTypes.Select(
                type =>
                {
                    var providerAttribute = type.GetCustomAttribute<RegistryObjectProviderAttribute>();
                    if (providerAttribute is null) return ((Type, RegistryObjectProviderAttribute)?)null;

                    return (type, providerAttribute);
                }
            ).Where(x => x is not null)
            .Select(x => ((Type, RegistryObjectProviderAttribute))x!);

        Action<ContainerBuilder> action = _ => { };

        foreach (var (providerType, providerAttribute) in providerTypesAndAttributes)
        {
            var interfaces = providerType.GetInterfaces();
            var isPreRegisterProvider = Array.Exists(interfaces, t => t.GUID.Equals(typeof(IPreRegisterProvider).GUID));
            var isMainRegisterProvider =
                Array.Exists(interfaces, t => t.GUID.Equals(typeof(IMainRegisterProvider).GUID));
            var isPostRegisterProvider =
                Array.Exists(interfaces, t => t.GUID.Equals(typeof(IPostRegisterProvider).GUID));

            if (isPreRegisterProvider)
            {
                action += builder => builder.RegisterType(providerType)
                    .Keyed<IPreRegisterProvider>(providerAttribute.RegistryId)
                    .WithMetadata(ModTag.MetadataName, new ModTag(isRootMod, modId));
            }

            if (isMainRegisterProvider)
            {
                action += builder => builder.RegisterType(providerType)
                    .Keyed<IMainRegisterProvider>(providerAttribute.RegistryId)
                    .WithMetadata(ModTag.MetadataName, new ModTag(isRootMod, modId));
            }

            if (isPostRegisterProvider)
            {
                action += builder => builder.RegisterType(providerType)
                    .Keyed<IPostRegisterProvider>(providerAttribute.RegistryId)
                    .WithMetadata(ModTag.MetadataName, new ModTag(isRootMod, modId));
            }
        }

        return action;
    }

    private static Action<ContainerBuilder> LoadByCustomProvider(Assembly modAssembly)
    {
        var providers = modAssembly.ExportedTypes.Where(type =>
        {
            var providerAttribute = type.GetCustomAttribute<AutofacProviderAttribute>();
            return providerAttribute is not null;
        });

        Action<ContainerBuilder> action = _ => { };
        foreach (var providerType in providers)
        {
            var provider = Activator.CreateInstance(providerType) as IAutofacProvider;
            Logger.AssertAndThrow(provider is not null, $"Failed to create instance of {providerType.FullName}",
                nameof(ModManager));

            action += builder => provider.Register(builder);
        }

        return action;
    }

    /// <summary>
    ///     Get the instance of a loaded mod
    /// </summary>
    /// <param name="modId">Id of the mod</param>
    /// <returns>Instance of the mod</returns>
    public IMod GetLoadedMod(ushort modId)
    {
        return _loadedMods[modId];
    }

    /// <summary>
    ///     Check whether or not a mod is a root mod
    /// </summary>
    /// <param name="modId">id of the mod to check</param>
    /// <returns>True if its a root mod; false if not or the mod is not present</returns>
    public bool IsRootMod(ushort modId)
    {
        return _loadedRootMods.Contains(modId);
    }

    public void ProcessRegistry(bool loadRootMods, LoadPhase loadPhase)
    {
        var modsToLoad = loadRootMods
            ? _loadedMods.Keys.Where(id => _loadedRootMods.Contains(id)).ToArray()
            : _loadedMods.Keys.Where(id => !_loadedRootMods.Contains(id)).ToArray();

        if (loadPhase.HasFlag(LoadPhase.Pre))
        {
            foreach (var modId in modsToLoad)
            {
                _loadedMods[modId].PreLoad();
            }
        }

        if (loadPhase.HasFlag(LoadPhase.Main))
        {
            RegistryManager.RegistryPhase = RegistryPhase.Categories;
            foreach (var modId in modsToLoad)
            {
                _loadedMods[modId].Load();

                var modStringId = _loadedModManifests[modId].Identifier;
                if (ModLifetimeScope.TryResolveKeyed<IRegistryProvider>(modStringId, out var registryProvider))
                {
                    registryProvider.Register(ModLifetimeScope, modId);
                }
            }

            RegistryManager.RegistryPhase = RegistryPhase.Objects;
            RegistryManager.ProcessRegistries(modsToLoad.Select(i => _loadedModManifests[i].Identifier).ToArray());
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
    public IEnumerable<(string modId, Version modVersion, IMod mod)> GetLoadedMods()
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
    public void UnloadMods(bool unloadRootMods)
    {
        var modsToRemove = !unloadRootMods
            ? _loadedMods.Where(x => !_loadedRootMods.Contains(x.Key)).Select(x => x.Key).ToArray()
            : _loadedMods.Keys.ToArray();

        RegistryManager.Clear(modsToRemove);

        FreeMods(modsToRemove);

        DestroyModLifetimeScope();
        RegistryManager.PostUnRegister();

        if (unloadRootMods)
        {
            DestroyRootLifetimeScope();
        }

        //At this point no reference to any type of the mods to unload should remain in the engine and root mods (if any present)
        //To ensure proper unloading of the mod assemblies
        if (_modLoadContext is { IsAllocated: true })
            WaitForUnloading(_modLoadContext);

        if (unloadRootMods && _rootLoadContext is { IsAllocated: true })
            WaitForUnloading(_rootLoadContext);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DestroyRootLifetimeScope()
    {
        _rootLifetimeScope?.Dispose();
        _rootLifetimeScope = null;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DestroyModLifetimeScope()
    {
        _modLifetimeScope?.Dispose();
        _modLifetimeScope = null;
    }

    // ReSharper disable once ParameterTypeCanBeEnumerable.Local; Change to IEnumerable<ushort> slows down the foreach loop
    private void FreeMods(ushort[] modsToUnload)
    {
        //Removes the mod reference and call unload
        foreach (var id in modsToUnload)
        {
            if (Logger.AssertAndLog(_loadedMods.Remove(id, out var mod),
                    $"Failed to remove and unload mod with numeric id {id}", "Modding", LogImportance.Warning))
                mod?.Unload();

            if (_loadedModArchives.Remove(id, out var archive)) archive.Dispose();

            _loadedModManifests.Remove(id);
            _loadedRootMods.Remove(id);
        }
    }

    public void SearchMods(IEnumerable<DirectoryInfo>? additionalModDirectories = null)
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
            _modManifests.Add(coreManifest.Identifier, new List<ModManifest>());

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StartLoadContextUnloading(GCHandle loadContextReference)
    {
        if (loadContextReference.Target is SharedAssemblyLoadContext loadContext)
        {
            loadContext.Unload();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference HandleToWeakReference(GCHandle handle)
    {
        return new WeakReference(handle.Target);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WaitForUnloading(GCHandle loadContextReference)
    {
        var reference = HandleToWeakReference(loadContextReference);
        StartLoadContextUnloading(loadContextReference);

        loadContextReference.Free();

        for (var i = 0; i < MaxUnloadTries && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        if (!reference.IsAlive)
        {
            Logger.WriteLog("Unloaded LoadContext", LogImportance.Info, "Modding");
            return;
        }

        Logger.WriteLog("Failed to unload assemblies", LogImportance.Warning, "Modding");

        Console.ReadLine();
    }

    /// <summary>
    ///     Check if the given set of mods is compatible to the loaded mods
    /// </summary>
    /// <param name="infoAvailableMods"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public bool ModsCompatible(IEnumerable<(string modId, Version version)> infoAvailableMods)
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
    public Stream GetResourceFileStream(Identification resource)
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

    public bool FileExists(ushort modId, string location)
    {
        if (!_loadedModArchives.TryGetValue(modId, out var archive)) return false;

        return archive.GetEntry(location) is not null;
    }

    private void LoadDll(SharedAssemblyLoadContext loadContext, ZipArchiveEntry entry)
    {
        using var zipStream = entry.Open();
        using var memoryStream = new MemoryStream();
        zipStream.CopyTo(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        loadContext.CustomLoadFromStream(memoryStream);
    }
}

[Flags]
public enum LoadPhase
{
    None = 0,
    Pre = 1,
    Main = 2,
    Post = 4
}

public record ModTag(bool IsRootMod, string Identifier)
{
    public const string MetadataName = "ModTag";
}