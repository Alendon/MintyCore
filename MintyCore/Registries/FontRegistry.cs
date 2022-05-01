using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.UI;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///     <see cref="IRegistry" /> to register Fonts
/// </summary>
[Registry("font", "fonts")]
public class FontRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Font;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

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
        if (Engine.HeadlessModeActive)
            return;
        FontHandler.RemoveFont(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }


    /// <inheritdoc />
    public void Clear()
    {
        FontHandler.Clear();
        ClearRegistryEvents();
    }

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    /// <summary>
    ///     Method to register a font family
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId">Id of the mod registering the font</param>
    /// <param name="stringIdentifier">String identifier of the font</param>
    /// <param name="fileName">Filename of the font</param>
    /// <returns><see cref="Identification" /> of the font</returns>
    [Obsolete]
    public static Identification RegisterFontFamily(ushort modId, string stringIdentifier, string fileName)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Font, stringIdentifier, fileName);
        if (Engine.HeadlessModeActive)
            return id;
        FontHandler.LoadFont(id);
        return id;
    }

    /// <summary>
    /// Register a font family
    /// Used by the source generator for <see cref="Registries.RegisterFontFamilyAttribute"/>
    /// </summary>
    /// <param name="id">Id of the font</param>
    /// <param name="fontInfo">Placeholder info</param>
    [RegisterMethod(ObjectRegistryPhase.Main, RegisterMethodOptions.HasFile)]
    public static void RegisterFontFamily(Identification id, FontInfo fontInfo)
    {
        if (Engine.HeadlessModeActive)
            return;
        FontHandler.LoadFont(id);
    }

    /// <summary>
    /// Register a font family
    /// Used by the source generator
    /// </summary>
    /// <param name="id">Id of the font</param>
    [RegisterMethod(ObjectRegistryPhase.Main, RegisterMethodOptions.HasFile)]
    public static void RegisterFontFamilyFile(Identification id)
    {
        if (Engine.HeadlessModeActive)
            return;
        FontHandler.LoadFont(id);
    }
}

/// <summary>
/// Placeholder struct to be able to register fonts in code
/// </summary>
public struct FontInfo
{
}