using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers;

[PublicAPI]
public interface IIntermediateDataManager
{
    void RegisterIntermediateData(Identification intermediateDataId,
        IntermediateDataRegistryWrapper intermediateDataRegistryWrapper);

    IntermediateData GetNewIntermediateData(Identification intermediateDataId);
    void RecycleIntermediateData(Identification intermediateDataId, IntermediateData data);


    void SetCurrentData(Identification intermediateDataId, IntermediateData originalData);
    IEnumerable<Identification> GetRegisteredIntermediateDataIds();
}