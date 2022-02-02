using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using MintyCore.Identifications;
using MintyCore.UI;
using MintyCore.Utils;

namespace MintyCore.Registries;

public class FontRegistry : IRegistry
{
    public ushort RegistryId => RegistryIDs.Font;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    public static event Action OnRegister;
    
    public void PreRegister()
    {
    }

    public void Register()
    {
        OnRegister();
    }

    public static Identification RegisterFont(ushort modId, string stringIdentifier, string fileName, uint fontSize = 36U)
    {
        Identification id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Font, stringIdentifier, fileName);
        FontHandler.LoadFont(id, fontSize);
        return id;
    }

    public void PostRegister()
    {
    }

    public void Clear()
    {
        FontHandler.Clear();
        OnRegister = delegate {  };
    }

    public void ClearRegistryEvents()
    {
       OnRegister = delegate {  };
    }
}