using MintyCore.Graphics.Render.Managers;

namespace MintyCore.Graphics.Render.Data.RegistryWrapper;

public abstract class IntermediateDataRegistryWrapper
{
    public abstract IntermediateData CreateIntermediateData(IIntermediateDataManager intermediateDataManager);
}