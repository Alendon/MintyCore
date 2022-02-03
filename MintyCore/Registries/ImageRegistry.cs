using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.UI;
using MintyCore.Utils;
using SixLabors.ImageSharp;

namespace MintyCore.Registries;

/// <summary>
/// <see cref="IRegistry"/> for <see cref="Image{Rgba32}"/>
/// </summary>
public class ImageRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Image;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();
    
    ///<summary/>
    public static event Action OnRegister = delegate {};

    /// <summary>
    /// Register a image
    /// </summary>
    /// <param name="modId">id of the mod registering the image</param>
    /// <param name="stringIdentifier">string identifier of the image</param>
    /// <param name="fileName">File name of the image</param>
    /// <returns><see cref="Identification"/> of the registered image</returns>
    public static Identification RegisterImage(ushort modId, string stringIdentifier, string fileName)
    {
        Identification id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Image, stringIdentifier, fileName);
        ImageHandler.AddImage(id);
        return id;
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
        
    }

    /// <inheritdoc />
    public void Clear()
    {
        ClearRegistryEvents();
        ImageHandler.Clear();
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate {  };
    }
}