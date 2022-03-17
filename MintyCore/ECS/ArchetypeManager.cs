using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     Class to manage archetype specific stuff at init and runtime
/// </summary>
public static class ArchetypeManager
{
    private static readonly Dictionary<Identification, ArchetypeContainer> _archetypes = new();

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

    internal static void AddArchetype(Identification archetypeId, ArchetypeContainer archetype,
        IEntitySetup? entitySetup)
    {
        _archetypes.TryAdd(archetypeId, archetype);
        if (entitySetup is not null)
            _entitySetups.Add(archetypeId, entitySetup);
    }

    internal static void ExtendArchetype(Identification archetypeId, IEnumerable<Identification> componentIDs)
    {
        //If the archetype is not yet present display a warning but proceed with adding it
        if (!_archetypes.ContainsKey(archetypeId))
        {
            Logger.WriteLog($"Tried to extend not present archetype {archetypeId}.", LogImportance.WARNING, "ECS");
            _archetypes.Add(archetypeId, new ArchetypeContainer(componentIDs));
            return;
        }

        var container = _archetypes[archetypeId];
        foreach (var componentId in componentIDs) container.ArchetypeComponents.Add(componentId);
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
    }

    internal static void RemoveArchetype(Identification objectId)
    {
        Logger.AssertAndLog(_archetypes.Remove(objectId), $"Archetype {objectId} to remove is not present", "ECS",
            LogImportance.WARNING);
        //Dont log if no entity setup could be removed as a entity setup is optional
        _entitySetups.Remove(objectId);
    }
}