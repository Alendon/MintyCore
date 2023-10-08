using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MintyCore.Utils;

namespace MintyCore.ECS;

public interface IArchetypeManager
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="archetypeId"></param>
    /// <param name="setup"></param>
    /// <returns></returns>
    bool TryGetEntitySetup(Identification archetypeId, [MaybeNullWhen(false)] out IEntitySetup setup);

    IArchetypeStorage CreateArchetypeStorage(Identification archetypeId);

    void AddArchetype(Identification archetypeId, ArchetypeContainer archetype,
        IEntitySetup? entitySetup);

    void ExtendArchetype(Identification archetypeId, IEnumerable<Identification> componentIDs,
        IEnumerable<string>? additionalDlls = null);

    /// <summary>
    ///     Get the ArchetypeContainer for a given archetype id
    /// </summary>
    /// <param name="archetypeId">id of the archetype</param>
    /// <returns>Container with the component ids of an archetype</returns>
    ArchetypeContainer GetArchetype(Identification archetypeId);

    /// <summary>
    ///     Get all registered archetype ids with their specific ArchetypeContainers
    /// </summary>
    /// <returns>ReadOnly Dictionary with archetype ids and ArchetypeContainers</returns>
    IReadOnlyDictionary<Identification, ArchetypeContainer> GetArchetypes();

    /// <summary>
    ///     Check if the archetype has a specific component
    /// </summary>
    /// <param name="archetypeId">The archetype to check</param>
    /// <param name="componentId">The component to check</param>
    /// <returns>Whether or not the component is present</returns>
    bool HasComponent(Identification archetypeId, Identification componentId);

    void Clear();
    void RemoveArchetype(Identification objectId);
    void RemoveGeneratedAssemblies();
}