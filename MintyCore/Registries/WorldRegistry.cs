using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Registries;

public class WorldRegistry : IRegistry
{
    public ushort RegistryId => RegistryIDs.World;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    public static event Action OnRegister = delegate { };
    public static event Action OnPostRegister = delegate {  };
    
    public static Identification RegisterWorld(ushort modId, string stringIdentifier, Func<bool, IWorld> worldCreateFunction)
    {
        Identification id = RegistryManager.RegisterObjectId(modId, RegistryIDs.World, stringIdentifier);
        WorldHandler.AddWorld(id, worldCreateFunction);
        return id;
    }

    public static void OverrideWorld(Identification worldId, Func<bool, IWorld> worldCreateFunction)
    {
        WorldHandler.AddWorld(worldId, worldCreateFunction);
    }
    
    public void PreRegister()
    {

    }

    public void Register()
    {
        OnRegister();
    }

    public void PostRegister()
    {
        OnPostRegister();
    }

    public void Clear()
    {
        WorldHandler.Clear();
        ClearRegistryEvents();
    }

    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
    }

    
}