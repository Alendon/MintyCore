using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all Archetypes
/// </summary>
[Registry("archetype")]
[PublicAPI]
public class ArchetypeRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Archetype;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => new[] {RegistryIDs.Component};
    
    ///<summary/>
    public required IArchetypeManager ArchetypeManager { private get; init; }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        ArchetypeManager.RemoveArchetype(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
        ArchetypeManager.RemoveGeneratedAssemblies();
    }

    /// <inheritdoc />
    public void Clear()
    {
        ArchetypeManager.Clear();
    }

    /// <summary>
    /// Register a Archetype
    /// Used by the SourceGenerator for the <see cref="Registries.RegisterArchetypeAttribute"/>
    /// </summary>
    /// <param name="archetypeId">Id of the archetype</param>
    /// <param name="info">Archetype info with the required information's</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterArchetype(Identification archetypeId, ArchetypeInfo info)
    {
        ArchetypeManager.AddArchetype(archetypeId, new ArchetypeContainer(info.Ids),
            info.EntitySetup);
    }
    
}

/// <summary>
/// Container storing the required information for adding an Archetype
/// </summary>
[PublicAPI]
public struct ArchetypeInfo
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="componentIIds">Ids of the components to use in the archetype</param>
    /// <param name="entitySetup">Optional entity setup which gets executed when the entity is created</param>
    public ArchetypeInfo(IEnumerable<Identification> componentIIds, IEntitySetup? entitySetup = null)
    {
        Ids = componentIIds;
        EntitySetup = entitySetup;
    }

    /// <summary>
    /// Ids of the components to use in the archetype
    /// </summary>
    public IEnumerable<Identification> Ids { get; set; }

    /// <summary>
    /// Optional entity setup which gets executed when the entity is created
    /// </summary>
    public IEntitySetup? EntitySetup { get; }
}