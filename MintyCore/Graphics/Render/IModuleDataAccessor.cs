using MintyCore.Graphics.Render.Data;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

public interface IModuleDataAccessor
{
    void SetInputDataConsumer(Identification inputData, Identification consumer);
    void SetIntermediateDataConsumer(Identification intermediateData, Identification consumer);
    void SetIntermediateDataProvider(Identification intermediateDataId, Identification inputModuleId);

    SingletonInputData<TData> GetSingletonInputData<TData>(Identification inputDataId,
        Identification inputModuleId) where TData : notnull;
}