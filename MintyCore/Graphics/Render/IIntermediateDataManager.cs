using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

[PublicAPI]
public interface IIntermediateDataManager
{
    void RegisterIntermediateData(Identification intermediateDataId, IntermediateDataRegistryWrapper intermediateDataRegistryWrapper);
    IntermediateDataSet GetNewIntermediateDataSet();
    void RecycleIntermediateDataSet(IntermediateDataSet intermediateDataSet);
    void SetCurrentIntermediateDataSet(IntermediateDataSet intermediateSet);
    IntermediateDataSet? GetCurrentIntermediateDataSet();
    void SetIntermediateProvider(Identification inputModuleId, Identification intermediateDataId);
    void SetIntermediateConsumerInputModule(Identification inputModuleId, Identification intermediateDataId);
    
    /// <summary>
    /// Validate that for each intermediate data which has a consumer, there is also a provider
    /// </summary>
    void ValidateIntermediateDataProvided();
}