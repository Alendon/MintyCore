﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
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

    /// <summary />
    public static event Action OnPreRegister = delegate { };
    
    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="info"></param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterWorld(Identification id, WorldInfo info)
    {
        WorldHandler.AddWorld(id, info.WorldCreateFunction);
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
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        WorldHandler.RemoveWorld(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void Clear()
    {
        WorldHandler.Clear();
        ClearRegistryEvents();
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
    }
}

/// <summary>
/// Wrapper struct to register a new world
/// </summary>
public struct WorldInfo
{
    /// <summary>
    /// Function to create the world
    /// bool => isServerWorld
    /// </summary>
    public Func<bool, IWorld> WorldCreateFunction;
}