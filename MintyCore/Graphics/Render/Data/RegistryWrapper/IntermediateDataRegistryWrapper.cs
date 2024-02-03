using MintyCore.Graphics.Render.Managers;

namespace MintyCore.Graphics.Render.Data.RegistryWrapper;

public abstract class IntermediateDataRegistryWrapper
{
    public abstract IntermediateData CreateIntermediateData(IIntermediateDataManager intermediateDataManager);
}

public class IntermediateDataRegistryWrapper<TIntermediateData> : IntermediateDataRegistryWrapper
    where TIntermediateData : IntermediateData, new()
{
    public override IntermediateData CreateIntermediateData(IIntermediateDataManager intermediateDataManager)
    {
        return new TIntermediateData { IntermediateDataManager = intermediateDataManager };
    }
}