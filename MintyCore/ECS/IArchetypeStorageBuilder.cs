using System;
using System.Reflection;
using Autofac;
using MintyCore.Modding;
using MintyCore.Utils;

namespace MintyCore.ECS;

public interface IArchetypeStorageBuilder
{
    /// <summary>
    /// Generate a new implementation of IArchetypeStorage based on the given archetype.
    /// </summary>
    /// <param name="archetype"> The archetype to generate the storage for. </param>
    /// <param name="archetypeId"> ID of the archetype. </param>
    /// <param name="assemblyLoadContext">The load context the assembly was loaded in</param>
    /// <param name="createdAssembly">The object representation of the created assembly</param>
    /// <param name="createdFile">The optional created assembly file</param>
    /// <returns>Function that creates a instance of the storage</returns>
    Action<ContainerBuilder> GenerateArchetypeStorage(ArchetypeContainer archetype,
        Identification archetypeId, out SharedAssemblyLoadContext assemblyLoadContext, out Assembly createdAssembly,
        out string? createdFile);
}