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

[Registry("render_data")]
public class RenderDataRegistry(IRenderDataManager renderDataManager) : IRegistry
{
    public ushort RegistryId => RegistryIDs.RenderData;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();


    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterRenderTexture(Identification id, RenderTextureDescription textureData)
    {
        renderDataManager.RegisterRenderTexture(id, textureData);
    }

    public void UnRegister(Identification objectId)
    {
        renderDataManager.RemoveRenderTexture(objectId);
    }

    public void Clear()
    {
        renderDataManager.Clear();
    }
}