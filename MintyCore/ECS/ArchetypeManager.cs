using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     Class to manage archetype specific stuff at init and runtime
/// </summary>
public static class ArchetypeManager
{
    private static readonly Dictionary<Identification, ArchetypeContainer> _archetypes = new();
    private static readonly Dictionary<Identification, Func<IArchetypeStorage?>> _storageCreators = new();
    private static readonly Dictionary<Identification, HashSet<string>> _additionalDllDependencies = new();

    private static readonly Dictionary<Identification, WeakReference> _storageLoadContexts = new();
    private static readonly Dictionary<Identification, WeakReference> _storageAssemblyHandles = new();
    private static readonly Dictionary<Identification, string> _createdDllFiles = new();
    private static readonly Queue<Identification> _storagesToRemove = new();

    private static bool _archetypesCreated;

    /// <summary>
    ///     Stores the entity setup "methods" for each entity
    ///     <see cref="IEntitySetup" />
    /// </summary>
    private static readonly Dictionary<Identification, IEntitySetup> _entitySetups = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="archetypeId"></param>
    /// <param name="setup"></param>
    /// <returns></returns>
    public static bool TryGetEntitySetup(Identification archetypeId, [MaybeNullWhen(false)] out IEntitySetup setup)
    {
        return _entitySetups.TryGetValue(archetypeId, out setup);
    }


    internal static IArchetypeStorage CreateArchetypeStorage(Identification archetypeId)
    {
        if (!_archetypesCreated) GenerateStorages();

        var storage = _storageCreators[archetypeId]();
        Logger.AssertAndThrow(storage is not null, $"Failed to instantiate storage for archetype {archetypeId}", "ECS");
        return storage;
    }

    internal static void AddArchetype(Identification archetypeId, ArchetypeContainer archetype,
        IEntitySetup? entitySetup, IEnumerable<string>? additionalDlls = null)
    {
        _archetypes.Add(archetypeId, archetype);
        if (entitySetup is not null)
            _entitySetups.Add(archetypeId, entitySetup);
        if (additionalDlls is not null)
            _additionalDllDependencies.Add(archetypeId, new HashSet<string>(additionalDlls));
    }

    internal static void ExtendArchetype(Identification archetypeId, IEnumerable<Identification> componentIDs,
        IEnumerable<string>? additionalDlls = null)
    {
        //If the archetype is not yet present display a warning but proceed with adding it
        if (!_archetypes.ContainsKey(archetypeId))
        {
            Logger.WriteLog($"Tried to extend not present archetype {archetypeId}.", LogImportance.Warning, "ECS");
            _archetypes.Add(archetypeId, new ArchetypeContainer(componentIDs));
            return;
        }

        Logger.AssertAndThrow(!_storageCreators.ContainsKey(archetypeId),
            $"Extending an archetype which has already been source generated {archetypeId}", "ECS");

        var container = _archetypes[archetypeId];
        foreach (var componentId in componentIDs) container.ArchetypeComponents.Add(componentId);

        if (additionalDlls is null) return;


        if (_additionalDllDependencies.TryGetValue(archetypeId, out var dlls))
            dlls.UnionWith(additionalDlls);
        else
            _additionalDllDependencies.Add(archetypeId, new HashSet<string>(additionalDlls));
    }

    /// <summary>
    ///     Get the ArchetypeContainer for a given archetype id
    /// </summary>
    /// <param name="archetypeId">id of the archetype</param>
    /// <returns>Container with the component ids of an archetype</returns>
    public static ArchetypeContainer GetArchetype(Identification archetypeId)
    {
        return _archetypes[archetypeId];
    }

    /// <summary>
    ///     Get all registered archetype ids with their specific ArchetypeContainers
    /// </summary>
    /// <returns>ReadOnly Dictionary with archetype ids and ArchetypeContainers</returns>
    public static IReadOnlyDictionary<Identification, ArchetypeContainer> GetArchetypes()
    {
        return _archetypes;
    }

    /// <summary>
    ///     Check if the archetype has a specific component
    /// </summary>
    /// <param name="archetypeId">The archetype to check</param>
    /// <param name="componentId">The component to check</param>
    /// <returns>Whether or not the component is present</returns>
    public static bool HasComponent(Identification archetypeId, Identification componentId)
    {
        return _archetypes.ContainsKey(archetypeId) &&
               _archetypes[archetypeId].ArchetypeComponents.Contains(componentId);
    }

    internal static void Clear()
    {
        _archetypes.Clear();
        _entitySetups.Clear();
        _storageCreators.Clear();
        _additionalDllDependencies.Clear();
        _createdDllFiles.Clear();
        _storageLoadContexts.Clear();
        _storagesToRemove.Clear();
        _storageAssemblyHandles.Clear();
    }

    internal static void RemoveArchetype(Identification objectId)
    {
        Logger.AssertAndLog(_archetypes.Remove(objectId), $"Archetype {objectId} to remove is not present", "ECS",
            LogImportance.Warning);
        //Dont log if no entity setup could be removed as a entity setup is optional
        _entitySetups.Remove(objectId);
    }

    internal static void RemoveGeneratedAssemblies()
    {
        if (!_archetypesCreated) return;

        var ids = _storageCreators.Keys.ToArray();
        _storageCreators.Clear();

        foreach (var objectId in ids)
        {
            _storageLoadContexts.Remove(objectId, out var loadContext);
            _storageAssemblyHandles.Remove(objectId, out var assemblyHandle);

            Logger.AssertAndThrow(loadContext is not null, "Weak reference is null", "ECS");
            Logger.AssertAndThrow(assemblyHandle is not null, "Weak reference is null", "ECS");

            UnloadAssemblyLoadContext(loadContext);

            for (var i = 0; i < 10 && (loadContext.IsAlive || assemblyHandle.IsAlive); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Logger.AssertAndLog(!assemblyHandle.IsAlive,
                "Failed to unload generated archetype storage assembly",
                "ECS", LogImportance.Error);


            _additionalDllDependencies.Remove(objectId);
            if (!_createdDllFiles.Remove(objectId, out var filePath) || assemblyHandle.IsAlive) continue;

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                Logger.WriteLog($"No generated dll file for {objectId} found. Deleted by the user?",
                    LogImportance.Warning, "ECS");
            else
                try
                {
                    fileInfo.Delete();
                }
                catch (UnauthorizedAccessException)
                {
                    Logger.WriteLog(
                        $"Failed to delete file {fileInfo} caused by an unauthorized access. Known problem, debug/testing mode only",
                        LogImportance.Warning, "ECS");
                }
                catch (Exception e)
                {
                    Logger.WriteLog($"Failed to delete file {fileInfo}: {e}", LogImportance.Error, "ECS");
                }
        }


        _archetypesCreated = false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UnloadAssemblyLoadContext(WeakReference loadContext)
    {
        if (loadContext.Target is AssemblyLoadContext context) context.Unload();
    }

    private static void GenerateStorages()
    {
        if (_archetypesCreated) return;

        ConcurrentBag<(Identification id, Func<IArchetypeStorage?> createFunc, AssemblyLoadContext assemblyLoadContext,
            Assembly
            createdAssembly, string? createdFile)> createdStorages = new();


        var all = Stopwatch.StartNew();

        Parallel.ForEach(_archetypes, pair =>
        {
            Logger.WriteLog($"Generating storage for {pair.Key}", LogImportance.Info, "ECS");

            _additionalDllDependencies.TryGetValue(pair.Key, out var additionalDlls);

            var sw = Stopwatch.StartNew();
            var createFunc = ArchetypeStorageBuilder.GenerateArchetypeStorage(pair.Value, pair.Key,
                additionalDlls,
                out var assemblyLoadContext, out var createdAssembly, out var createdFile);
            sw.Stop();
            Logger.WriteLog($"Generated storage for {pair.Key} in {sw.ElapsedMilliseconds}ms", LogImportance.Info,
                "ECS");

            createdStorages.Add((pair.Key, createFunc, assemblyLoadContext, createdAssembly, createdFile));
        });

        all.Stop();
        Logger.WriteLog($"Generated all storages in {all.ElapsedMilliseconds}ms", LogImportance.Info, "ECS");

        foreach (var (id, createFunc, assemblyLoadContext, createdAssembly, createdFile) in createdStorages)
        {
            _storageCreators.Add(id, createFunc);
            _storageLoadContexts.Add(id, new WeakReference(assemblyLoadContext));
            if (createdFile is not null)
                _createdDllFiles.Add(id, createdFile);
            _storageAssemblyHandles.Add(id, new WeakReference(createdAssembly));
        }

        _archetypesCreated = true;
    }
}