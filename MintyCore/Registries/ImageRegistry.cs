using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.UI;
using MintyCore.Utils;
using SixLabors.ImageSharp;

namespace MintyCore.Registries;

/// <summary>
///     <see cref="IRegistry" /> for <see cref="Image{TPixel}" />
/// </summary>
public class ImageRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Image;

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
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <inheritdoc />
    public void Clear()
    {
        ClearRegistryEvents();
        ImageHandler.Clear();
    }

    /// <summary>
    ///     Register a image
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId">id of the mod registering the image</param>
    /// <param name="stringIdentifier">string identifier of the image</param>
    /// <param name="fileName">File name of the image</param>
    /// <returns><see cref="Identification" /> of the registered image</returns>
    public static Identification RegisterImage(ushort modId, string stringIdentifier, string fileName)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Image, stringIdentifier, fileName);
        ImageHandler.AddImage(id);
        return id;
    }

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };
}