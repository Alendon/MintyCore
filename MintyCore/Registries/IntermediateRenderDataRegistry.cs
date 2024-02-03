using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("intermediate_render_data")]
public class IntermediateRenderDataRegistry : IRegistry
{
    public ushort RegistryId => RegistryIDs.IntermediateRenderData;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    public required IIntermediateDataManager IntermediateDataManager { private get; [UsedImplicitly] init; }

    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterIntermediateRenderDataByType<T>(Identification id) where T : IntermediateData, new()
    {
        IntermediateDataManager.RegisterIntermediateData<T>(id);
    }

    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterIntermediateRenderDataByProperty(Identification id,
        IntermediateDataRegistryWrapper registryWrapper)
    {
        IntermediateDataManager.RegisterIntermediateData(id, registryWrapper);
    }


    public void UnRegister(Identification objectId)
    {
        IntermediateDataManager.UnRegisterIntermediateData(objectId);
    }

    public void Clear()
    {
        IntermediateDataManager.Clear();
    }
}