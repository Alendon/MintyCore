using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Veldrid.Texture" />
/// </summary>
public class TextureRegistry : IRegistry
{
    /// <summary />
    public delegate void RegisterDelegate();

    /// <inheritdoc />
    public void PreRegister()
    {
    }

    /// <inheritdoc />
    public void Register()
    {
        Logger.WriteLog("Registering Textures", LogImportance.INFO, "Registry");
        OnRegister.Invoke();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
    }

    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Textures", LogImportance.INFO, "Registry");
        OnRegister = delegate { };
        TextureHandler.Clear();
    }
        
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
    }

    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Texture;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => new[] { RegistryIDs.DescriptorSet };

    /// <summary />
    public static event RegisterDelegate OnRegister = delegate { };

    /// <summary>
    ///     Register a <see cref="Veldrid.Texture" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Veldrid.Texture" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Veldrid.Texture" /></param>
    /// <param name="textureName">The file name of the texture</param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="Veldrid.Texture" /></returns>
    public static Identification RegisterTexture(ushort modId, string stringIdentifier, string textureName,
        bool mipMapping = true, IResampler? resampler = null, bool cpuOnly = false, bool flipY = false)
    {
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Texture, stringIdentifier, textureName);
        TextureHandler.AddTexture(id, mipMapping, resampler ?? LanczosResampler.Lanczos2, cpuOnly, flipY);
        return id;
    }
}