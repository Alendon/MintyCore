using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     Container class to store a set of componentIDs for a specific archetype
/// </summary>
public class ArchetypeContainer
{
    /// <summary>
    ///     Create a new ArchetypeContainer with a HashSet containing the archetype component ids
    /// </summary>
    /// <param name="archetypeComponents">HashSet containing the component ids</param>
    public ArchetypeContainer(HashSet<Identification> archetypeComponents)
    {
        ArchetypeComponents = archetypeComponents;
    }

    /// <summary>
    ///     Create a new ArchetypeContainer with an Enumerable of component ids
    /// </summary>
    /// <param name="archetypeComponents">Enumerable with component ids</param>
    public ArchetypeContainer(IEnumerable<Identification> archetypeComponents)
    {
        ArchetypeComponents = new HashSet<Identification>(archetypeComponents);
    }
        
    /// <summary>
    ///     Create a new ArchetypeContainer with an array of component ids
    /// </summary>
    /// <param name="archetypeComponents">array with component ids</param>
    public ArchetypeContainer(params Identification[] archetypeComponents)
    {
        ArchetypeComponents = new HashSet<Identification>(archetypeComponents);
    }

    internal HashSet<Identification> ArchetypeComponents { get; }

    /// <summary>
    ///     Add component ids to the archetype container
    /// </summary>
    /// <param name="components">component ids to add</param>
    public void AddComponents(params Identification[] components)
    {
        foreach (var entry in components) ArchetypeComponents.Add(entry);
    }
}