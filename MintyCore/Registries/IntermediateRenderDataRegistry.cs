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

/// <summary>
/// Registry for all intermediate render data
/// </summary>
[Registry("intermediate_render_data")]
public class IntermediateRenderDataRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.IntermediateRenderData;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    /// <summary/>
    public required IIntermediateDataManager IntermediateDataManager { private get; [UsedImplicitly] init; }

    /// <summary>
    /// Register a intermediate render data by type
    /// </summary>
    /// <param name="id"> Id of the intermediate render data</param>
    /// <typeparam name="T"> Type of the intermediate render data</typeparam>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterIntermediateRenderDataByType<T>(Identification id) where T : IntermediateData, new()
    {
        IntermediateDataManager.RegisterIntermediateData<T>(id);
    }

    /// <summary>
    ///   Register a intermediate render data by property
    /// </summary>
    /// <param name="id"> Id of the intermediate render data</param>
    /// <param name="registryWrapper"> Wrapper for the intermediate render data</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterIntermediateRenderDataByProperty(Identification id,
        IntermediateDataRegistryWrapper registryWrapper)
    {
        IntermediateDataManager.RegisterIntermediateData(id, registryWrapper);
    }


    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        IntermediateDataManager.UnRegisterIntermediateData(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        IntermediateDataManager.Clear();
    }
}