﻿using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
/// Class to register additional <see cref="IWorld"/>
/// </summary>
public class WorldRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.World;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary>
    /// Register a new <see cref="IWorld"/>
    /// Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="IWorld" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="IWorld" /></param>
    /// <param name="worldCreateFunction">Function which takes a bool (representing if its a server world) and returning a new <see cref="IWorld"/> instance </param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="IWorld" /></returns>
    public static Identification RegisterWorld(ushort modId, string stringIdentifier,
        Func<bool, IWorld> worldCreateFunction)
    {
        Identification id = RegistryManager.RegisterObjectId(modId, RegistryIDs.World, stringIdentifier);
        WorldHandler.AddWorld(id, worldCreateFunction);
        return id;
    }

    /// <summary>
    /// Override a previously registered <see cref="IWorld"/>
    /// </summary>
    /// <param name="worldId">Id of the world</param>
    /// <param name="worldCreateFunction">Function which takes a bool (representing if its a server world) and returning a new <see cref="IWorld"/> instance </param>
    public static void OverrideWorld(Identification worldId, Func<bool, IWorld> worldCreateFunction)
    {
        WorldHandler.AddWorld(worldId, worldCreateFunction);
    }

    /// <inheritdoc />
    public void PreRegister()
    {
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