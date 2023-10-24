using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;
using Autofac;
using MintyCore.Modding;
using MintyCore.Utils;

namespace MintyCore.ECS.Implementations;

/// <summary>
///     Class to manage archetype specific stuff at init and runtime
/// </summary>
[Singleton<IArchetypeManager>]
public class ArchetypeManager : IArchetypeManager
{
    private readonly Dictionary<Identification, ArchetypeContainer> _archetypes = new();

    private readonly Dictionary<Identification, WeakReference> _storageLoadContexts = new();
    private readonly Dictionary<Identification, WeakReference> _storageAssemblyHandles = new();
    private readonly Dictionary<Identification, string> _createdDllFiles = new();
    private readonly Queue<Identification> _storagesToRemove = new();
    
    public required IArchetypeStorageBuilder ArchetypeStorageBuilder { private get; init; }
    public required ILifetimeScope LifetimeScope { private get; init; }
    private ILifetimeScope? _archetypeStorageScope;

    private bool _archetypesCreated;

    /// <summary>
    ///     Stores the entity setup "methods" for each entity
    ///     <see cref="IEntitySetup" />
    /// </summary>
    private readonly Dictionary<Identification, IEntitySetup> _entitySetups = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="archetypeId"></param>
    /// <param name="setup"></param>
    /// <returns></returns>
    public bool TryGetEntitySetup(Identification archetypeId, [MaybeNullWhen(false)] out IEntitySetup setup)
    {
        return _entitySetups.TryGetValue(archetypeId, out setup);
    }


    public IArchetypeStorage CreateArchetypeStorage(Identification archetypeId)
    {
        if (!_archetypesCreated) GenerateStorages();
        Logger.AssertAndThrow(_archetypeStorageScope is not null, "Archetype storage scope is null", "ECS");

        var storage = _archetypeStorageScope.ResolveKeyed<IArchetypeStorage>(archetypeId);
        Logger.AssertAndThrow(storage is not null, $"Failed to instantiate storage for archetype {archetypeId}", "ECS");
        return storage;
    }

    public void AddArchetype(Identification archetypeId, ArchetypeContainer archetype,
        IEntitySetup? entitySetup)
    {
        _archetypes.Add(archetypeId, archetype);
        if (entitySetup is not null)
            _entitySetups.Add(archetypeId, entitySetup);
    }

    /// <summary>
    ///     Get the ArchetypeContainer for a given archetype id
    /// </summary>
    /// <param name="archetypeId">id of the archetype</param>
    /// <returns>Container with the component ids of an archetype</returns>
    public ArchetypeContainer GetArchetype(Identification archetypeId)
    {
        return _archetypes[archetypeId];
    }

    /// <summary>
    ///     Get all registered archetype ids with their specific ArchetypeContainers
    /// </summary>
    /// <returns>ReadOnly Dictionary with archetype ids and ArchetypeContainers</returns>
    public IReadOnlyDictionary<Identification, ArchetypeContainer> GetArchetypes()
    {
        return _archetypes;
    }

    /// <summary>
    ///     Check if the archetype has a specific component
    /// </summary>
    /// <param name="archetypeId">The archetype to check</param>
    /// <param name="componentId">The component to check</param>
    /// <returns>Whether or not the component is present</returns>
    public bool HasComponent(Identification archetypeId, Identification componentId)
    {
        return _archetypes.ContainsKey(archetypeId) &&
               _archetypes[archetypeId].ArchetypeComponents.Contains(componentId);
    }

    public void Clear()
    {
        _archetypeStorageScope?.Dispose();
        _archetypeStorageScope = null;
        
        _archetypes.Clear();
        _entitySetups.Clear();
        _createdDllFiles.Clear();
        _storageLoadContexts.Clear();
        _storagesToRemove.Clear();
        _storageAssemblyHandles.Clear();
    }

    /// <inheritdoc />
    public void RemoveArchetype(Identification objectId)
    {
        Logger.AssertAndLog(_archetypes.Remove(objectId), $"Archetype {objectId} to remove is not present", "ECS",
            LogImportance.Warning);
        //Dont log if no entity setup could be removed as a entity setup is optional
        _entitySetups.Remove(objectId);
    }

    /// <inheritdoc />
    public void RemoveGeneratedAssemblies()
    {
        if (!_archetypesCreated) return;

        _archetypeStorageScope?.Dispose();
        _archetypeStorageScope = null;

        foreach (var objectId in _archetypes.Keys)
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


            if (!_createdDllFiles.Remove(objectId, out var filePath) || assemblyHandle.IsAlive) continue;

            DeleteAssemblyFile(filePath, objectId);
        }


        _archetypesCreated = false;
    }

    private static void DeleteAssemblyFile(string filePath, Identification objectId)
    {
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
            catch (Exception e) when (e is SecurityException or IOException)
            {
                Logger.WriteLog($"Failed to delete file {fileInfo}: {e}", LogImportance.Error, "ECS");
            }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UnloadAssemblyLoadContext(WeakReference loadContext)
    {
        if (loadContext.Target is SharedAssemblyLoadContext context) context.Unload();
    }

    private void GenerateStorages()
    {
        if (_archetypesCreated) return;

        ConcurrentBag<(Identification id, Action<ContainerBuilder> containerBuilder, SharedAssemblyLoadContext
            assemblyLoadContext,
            Assembly
            createdAssembly, string? createdFile)> createdStorages = new();


        var all = Stopwatch.StartNew();

        Parallel.ForEach(_archetypes, pair =>
        {
            Logger.WriteLog($"Generating storage for {pair.Key}", LogImportance.Info, "ECS");

            var sw = Stopwatch.StartNew();
            var containerBuilder = ArchetypeStorageBuilder.GenerateArchetypeStorage(pair.Value, pair.Key,
                out var assemblyLoadContext, out var createdAssembly, out var createdFile);
            sw.Stop();
            Logger.WriteLog($"Generated storage for {pair.Key} in {sw.ElapsedMilliseconds}ms", LogImportance.Info,
                "ECS");

            createdStorages.Add((pair.Key, containerBuilder, assemblyLoadContext, createdAssembly, createdFile));
        });

        all.Stop();
        Logger.WriteLog($"Generated all storages in {all.ElapsedMilliseconds}ms", LogImportance.Info, "ECS");

        var accumulatedContainerBuilderAction = (ContainerBuilder _) => { };
        foreach (var (id, containerBuilder, assemblyLoadContext, createdAssembly, createdFile) in createdStorages)
        {
            accumulatedContainerBuilderAction += containerBuilder;
            _storageLoadContexts.Add(id, new WeakReference(assemblyLoadContext));
            if (createdFile is not null)
                _createdDllFiles.Add(id, createdFile);
            _storageAssemblyHandles.Add(id, new WeakReference(createdAssembly));
        }

        _archetypeStorageScope = LifetimeScope.BeginLifetimeScope(accumulatedContainerBuilderAction);

        _archetypesCreated = true;
    }
}