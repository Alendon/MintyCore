using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
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
    
    public required IArchetypeManager ArchetypeManager { private get; init; }

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

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
        ClearRegistryEvents();
        ArchetypeManager.Clear();
    }

    /// <inheritdoc />
    public void PreRegister()
    {
        OnPreRegister();
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
        OnPostRegister();
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    /// <summary>
    /// Register a Archetype
    /// Used by the SourceGenerator for the <see cref="Registries.RegisterArchetypeAttribute"/>
    /// </summary>
    /// <param name="archetypeId">Id of the archetype</param>
    /// <param name="info">Archetype info with the required information's</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterArchetype(Identification archetypeId, ArchetypeInfo info)
    {
        ArchetypeManager.AddArchetype(archetypeId, new ArchetypeContainer(info.ComponentIDs),
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
    /// <param name="componentIDs">Ids of the components to use in the archetype</param>
    /// <param name="entitySetup">Optional entity setup which gets executed when the entity is created</param>
    public ArchetypeInfo(IEnumerable<Identification> componentIDs, IEntitySetup? entitySetup = null)
    {
        ComponentIDs = componentIDs;
        EntitySetup = entitySetup;
    }

    /// <summary>
    /// Ids of the components to use in the archetype
    /// </summary>
    public IEnumerable<Identification> ComponentIDs;

    /// <summary>
    /// Optional entity setup which gets executed when the entity is created
    /// </summary>
    public IEntitySetup? EntitySetup;
}