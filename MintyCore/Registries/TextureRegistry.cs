using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Texture" />
/// </summary>
public class TextureRegistry : IRegistry
{
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
        TextureHandler.RemoveTexture(objectId);
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
        Logger.WriteLog("Clearing Textures", LogImportance.INFO, "Registry");
        ClearRegistryEvents();
        TextureHandler.Clear();
    }


    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Texture;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => new[] { RegistryIDs.DescriptorSet };

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };


    /// <summary>
    ///     Register a <see cref="Texture" />
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Texture" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Texture" /></param>
    /// <param name="textureName">The file name of the texture</param>
    /// <param name="mipMapping">Whether or not mip levels should be generated</param>
    /// <param name="resampler">
    ///     Which resampler to choose for mip map creation
    ///     <seealso cref="SixLabors.ImageSharp.Processing.KnownResamplers" />
    /// </param>
    /// <param name="flipY">Whether or not the y axis of the texture should be flipped</param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="Texture" /></returns>
    public static Identification RegisterTexture(ushort modId, string stringIdentifier, string textureName,
        bool mipMapping = true, IResampler? resampler = null, bool flipY = false)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Texture, stringIdentifier, textureName);
        TextureHandler.AddTexture(id, mipMapping, resampler ?? LanczosResampler.Lanczos2, flipY);
        return id;
    }
}