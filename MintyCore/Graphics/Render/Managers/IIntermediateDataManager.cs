using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers;

[PublicAPI]
public interface IIntermediateDataManager
{
    void RegisterIntermediateData<TIntermediateData>(Identification intermediateDataId)
        where TIntermediateData : IntermediateData, new();

    void RegisterIntermediateData(Identification intermediateDataId, IntermediateDataRegistryWrapper registryWrapper);

    IntermediateData GetNewIntermediateData(Identification intermediateDataId);
    void RecycleIntermediateData(Identification intermediateDataId, IntermediateData data);


    void SetCurrentData(Identification intermediateDataId, IntermediateData currentData);
    IEnumerable<Identification> GetRegisteredIntermediateDataIds();
    IntermediateData? GetCurrentData(Identification intermediateId);
    void UnRegisterIntermediateData(Identification objectId);
    void Clear();
    Type GetIntermediateDataType(Identification intermediateDataId);
}