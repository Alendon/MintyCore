using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers;

[PublicAPI]
public interface IIntermediateDataManager
{
    void RegisterIntermediateData(Identification intermediateDataId, IntermediateDataRegistryWrapper intermediateDataRegistryWrapper);
    void SetIntermediateProvider(Identification inputModuleId, Identification intermediateDataId);
    void SetIntermediateConsumerInputModule(Identification inputModuleId, Identification intermediateDataId);
    
    /// <summary>
    /// Validate that for each intermediate data which has a consumer, there is also a provider
    /// </summary>
    void ValidateIntermediateDataProvided();

    IntermediateData GetNewIntermediateData(Identification intermediateDataId);
    void RecycleIntermediateData(Identification intermediateDataId, IntermediateData data);


    void SetCurrentData(Identification intermediateDataId, IntermediateData originalData);
}