using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.UI;
using MintyCore.Utils;

namespace MintyCore.Registries;

public class ImageRegistry : IRegistry
{
    public ushort RegistryId => RegistryIDs.Image;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    public static event Action OnRegister = delegate {};

    public static Identification RegisterImage(ushort modId, string stringIdentifier, string fileName)
    {
        Identification id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Image, stringIdentifier, fileName);
        ImageHandler.AddImage(id);
        return id;
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
        
    }

    public void Clear()
    {
        ClearRegistryEvents();
        ImageHandler.Clear();
    }

    public void ClearRegistryEvents()
    {
        OnRegister = delegate {  };
    }
}