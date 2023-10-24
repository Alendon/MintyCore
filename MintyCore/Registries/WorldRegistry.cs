using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
/// Class to register additional <see cref="IWorld"/>
/// </summary>
[Registry("world")]
[PublicAPI]
public class WorldRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.World;
    
    public required IWorldHandler WorldHandler { private get; init; }

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterWorld<TWorld>(Identification id) where TWorld : class, IWorld
    {
        WorldHandler.AddWorld<TWorld>(id);
    }

    /// <inheritdoc />
    public void PostRegister(ObjectRegistryPhase currentPhase)
    {
        if(currentPhase == ObjectRegistryPhase.Main)
            WorldHandler.CreateWorldLifetimeScope();
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        WorldHandler.RemoveWorld(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        WorldHandler.Clear();
    }
}