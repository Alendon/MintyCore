using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.UI;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
/// <see cref="IRegistry"/> to register Fonts
/// </summary>
public class FontRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Font;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();



    ///<summary/>
    public static event Action OnRegister = delegate {  };

    /// <inheritdoc />
    public void PreRegister()
    {
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <summary>
    /// Method to register a font family
    /// </summary>
    /// <param name="modId">Id of the mod registering the font</param>
    /// <param name="stringIdentifier">String identifier of the font</param>
    /// <param name="fileName">Filename of the font</param>
    /// <returns><see cref="Identification"/> of the font</returns>
    public static Identification RegisterFontFamily(ushort modId, string stringIdentifier, string fileName)
    {
        Identification id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Font, stringIdentifier, fileName);
        FontHandler.LoadFont(id);
        return id;
    }

    /// <inheritdoc />
    public void PostRegister()
    {
    }

    /// <inheritdoc />
    public void Clear()
    {
        FontHandler.Clear();
        OnRegister = delegate {  };
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
       OnRegister = delegate {  };
    }
}