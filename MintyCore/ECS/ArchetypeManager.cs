using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using MintyCore.Identifications;
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
        {
            _additionalDllDependencies.Add(archetypeId, new HashSet<string>(additionalDlls));
        }
    }

    internal static void ExtendArchetype(Identification archetypeId, IEnumerable<Identification> componentIDs,
        IEnumerable<string>? additionalDlls = null)
    {
        //If the archetype is not yet present display a warning but proceed with adding it
        if (!_archetypes.ContainsKey(archetypeId))
        {
            Logger.WriteLog($"Tried to extend not present archetype {archetypeId}.", LogImportance.WARNING, "ECS");
            _archetypes.Add(archetypeId, new ArchetypeContainer(componentIDs));
            return;
        }

        Logger.AssertAndThrow(!_storageCreators.ContainsKey(archetypeId),
            $"Extending an archetype which has already been source generated {archetypeId}", "ECS");

        var container = _archetypes[archetypeId];
        foreach (var componentId in componentIDs) container.ArchetypeComponents.Add(componentId);

        if (additionalDlls is not null)
        {
            if (_additionalDllDependencies.TryGetValue(archetypeId, out var dlls))
                dlls.UnionWith(additionalDlls);
            else
                _additionalDllDependencies.Add(archetypeId, new HashSet<string>(additionalDlls));
        }
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
            LogImportance.WARNING);
        //Dont log if no entity setup could be removed as a entity setup is optional
        _entitySetups.Remove(objectId);

        _storageCreators.Remove(objectId);
        _storagesToRemove.Enqueue(objectId);
    }

    internal static void RemoveGeneratedAssemblies()
    {
        while (_storagesToRemove.TryDequeue(out var objectId))
        {
            _storageLoadContexts.Remove(objectId, out var loadContext);
            _storageAssemblyHandles.Remove(objectId, out var assemblyHandle);
            
            Logger.AssertAndThrow(loadContext is not null, "Weak reference is null", "ECS");
            Logger.AssertAndThrow(assemblyHandle is not null, "Weak reference is null", "ECS");

            UnloadAssemblyLoadContext(loadContext);

            for (int i = 0; i < 10 && (loadContext.IsAlive || assemblyHandle.IsAlive); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Logger.AssertAndLog(!assemblyHandle.IsAlive,
                "Failed to unload generated archetype storage assembly",
                "ECS", LogImportance.ERROR);


            _additionalDllDependencies.Remove(objectId);
            if (_createdDllFiles.Remove(objectId, out var filePath) && !assemblyHandle.IsAlive)
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    Logger.WriteLog($"No generated dll file for {objectId} found. Deleted by the user?",
                        LogImportance.WARNING, "ECS");
                }
                else
                {
                    try
                    {
                        fileInfo.Delete();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Logger.WriteLog($"Failed to delete file {fileInfo} caused by an unauthorized access. Known problem, debug/testing mode only", LogImportance.WARNING ,"ECS");
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog($"Failed to delete file {fileInfo}: {e}", LogImportance.ERROR, "ECS");
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UnloadAssemblyLoadContext(WeakReference loadContext)
    {
        if (loadContext.Target is AssemblyLoadContext context)
        {
            context.Unload();
        }
    }

    internal static void GenerateStorages()
    {
        foreach (var (id, container) in _archetypes.Where(entry => !_storageCreators.ContainsKey(entry.Key)))
        {
            _additionalDllDependencies.TryGetValue(id, out var additionalDlls);
            var createFunc = ArchetypeStorageBuilder.GenerateArchetypeStorage(container, id, additionalDlls,
                out var assemblyLoadContext, out var createdAssembly, out var createdFile);

            //Store a weak reference for the assembly load context and the created assembly
            //By this the unloading process of the assembly can be tracked without keeping it alive
            _storageCreators.Add(id, createFunc);
            _storageLoadContexts.Add(id, new WeakReference(assemblyLoadContext));
            if (createdFile is not null)
                _createdDllFiles.Add(id, createdFile);
            _storageAssemblyHandles.Add(id, new WeakReference(createdAssembly));
        }
    }
}