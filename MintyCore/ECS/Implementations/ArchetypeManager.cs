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
using JetBrains.Annotations;
using MintyCore.Modding;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.ECS.Implementations;

/// <summary>
///     Class to manage archetype specific stuff at init and runtime
/// </summary>
[Singleton<IArchetypeManager>]
internal class ArchetypeManager : IArchetypeManager
{
    private readonly Dictionary<Identification, ArchetypeContainer> _archetypes = new();

    private readonly Dictionary<Identification, WeakReference> _storageLoadContexts = new();
    private readonly Dictionary<Identification, WeakReference> _storageAssemblyHandles = new();
    private readonly Dictionary<Identification, string> _createdDllFiles = new();
    private readonly Queue<Identification> _storagesToRemove = new();

    public required IArchetypeStorageBuilder ArchetypeStorageBuilder { private get; [UsedImplicitly] init; }
    public required ILifetimeScope LifetimeScope { private get; [UsedImplicitly] init; }
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
        if (_archetypeStorageScope is null)
            throw new InvalidOperationException("Archetype storage scope is null");

        var storage = _archetypeStorageScope.ResolveKeyed<IArchetypeStorage>(archetypeId);

        if (storage is null)
            throw new InvalidOperationException($"Failed to instantiate storage for archetype {archetypeId}");

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
        if (!_archetypes.Remove(objectId))
            Log.Warning("Archetype {ArchetypeId} to remove is not present", objectId);

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

            if (loadContext is null || assemblyHandle is null)
            {
                throw new MintyCoreException(
                    $"Failed to remove generated assembly for archetype {objectId}. Weak reference is null");
            }

            UnloadAssemblyLoadContext(loadContext);

            for (var i = 0; i < 10 && (loadContext.IsAlive || assemblyHandle.IsAlive); i++)
            {
#pragma warning disable S1215
                GC.Collect();
#pragma warning restore S1215

                GC.WaitForPendingFinalizers();
            }

            if (assemblyHandle.IsAlive)
                Log.Error("Failed to unload generated archetype storage assembly for {ArchetypeId}",
                    objectId);

            if (!_createdDllFiles.Remove(objectId, out var filePath) || assemblyHandle.IsAlive) continue;

            DeleteAssemblyFile(filePath, objectId);
        }


        _archetypesCreated = false;
    }

    private static void DeleteAssemblyFile(string filePath, Identification objectId)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            Log.Warning("No generated dll file for {ArchetypeId} found. Deleted by the user?", objectId);
        }
        else
        {
            try
            {
                fileInfo.Delete();
            }
            catch (UnauthorizedAccessException)
            {
                Log.Warning(
                    "Failed to delete file {File} caused by an unauthorized access. Known problem, debug/testing mode only",
                    fileInfo);
            }
            catch (Exception e) when (e is SecurityException or IOException)
            {
                Log.Error(e, "Failed to delete file {File}", fileInfo);
            }
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
            Log.Information("Generating storage for {ArchetypeId}", pair.Key);

            var sw = Stopwatch.StartNew();
            var containerBuilder = ArchetypeStorageBuilder.GenerateArchetypeStorage(pair.Value, pair.Key,
                out var assemblyLoadContext, out var createdAssembly, out var createdFile);
            sw.Stop();
            Log.Information("Generated storage for {ArchetypeId} in {Time}ms", pair.Key, sw.ElapsedMilliseconds);
            
            createdStorages.Add((pair.Key, containerBuilder, assemblyLoadContext, createdAssembly, createdFile));
        });

        all.Stop();
        Log.Information("Generated all storages in {ElapsedTime}ms", all.ElapsedMilliseconds);

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