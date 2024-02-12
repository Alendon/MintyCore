using System.Collections.Generic;
using System.Linq;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
/// Registry to manage render data
/// </summary>
[Registry("render_data")]
public class RenderDataRegistry(IRenderDataManager renderDataManager) : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.RenderData;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();


    /// <summary>
    ///   Register a render texture
    /// </summary>
    /// <param name="id"> Id of the render texture</param>
    /// <param name="textureData"> Description of the render texture</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterRenderTexture(Identification id, RenderTextureDescription textureData)
    {
        renderDataManager.RegisterRenderTexture(id, textureData);
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        renderDataManager.RemoveRenderTexture(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        renderDataManager.Clear();
    }
}