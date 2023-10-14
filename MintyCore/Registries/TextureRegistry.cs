using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Render;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Texture" />
/// </summary>
[Registry("texture", "textures", applicableGameType: GameType.Client)]
[PublicAPI]
public class TextureRegistry : IRegistry
{
    public required ITextureManager TextureManager { private get; init; }

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        if (Engine.HeadlessModeActive)
            return;
        TextureManager.RemoveTexture(objectId);
    }



    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Textures", LogImportance.Info, "Registry");
        TextureManager.Clear();
    }


    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Texture;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => new[] {RegistryIDs.DescriptorSet};


    /// <summary>
    /// Register a <see cref="Texture" />
    /// This method is used by the source generator for the auto registry
    /// </summary>
    /// <param name="id"></param>
    [RegisterMethod(ObjectRegistryPhase.Main, RegisterMethodOptions.HasFile)]
    public void RegisterTexture(Identification id)
    {
        if (Engine.HeadlessModeActive)
            return;
        TextureManager.AddTexture(id, true, LanczosResampler.Lanczos2, false);
    }
}