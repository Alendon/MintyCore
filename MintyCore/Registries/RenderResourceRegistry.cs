using System.Collections.Generic;
using System.Linq;
using MintyCore.Graphics.RenderGraphTest.RenderResources;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("render_resource")]
public class RenderResourceRegistry : IRegistry
{
    public ushort RegistryId => RegistryIDs.RenderResource;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();
    public void UnRegister(Identification objectId)
    {
        throw new System.NotImplementedException();
    }

    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterTextureResource(Identification id, TextureResourceDescription description)
    {
        throw new System.NotImplementedException();
    }

    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterSwapchainResourceDoNotUseThis(Identification id, object _)
    {
        throw new System.NotImplementedException();
    }
    
    public void Clear()
    {
        throw new System.NotImplementedException();
    }
}