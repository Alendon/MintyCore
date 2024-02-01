using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Utils;

namespace TestMod.Render;

public class TriangleMeshData : IntermediateData
{
    public TriangleMeshData(IIntermediateDataManager intermediateDataManager) : base(intermediateDataManager)
    {
        
    }

    public override void Clear()
    {
        throw new NotImplementedException();
    }

    public override Identification Identification { get; }
    
    public class TriangleMeshDataRegistryWrapper : IntermediateDataRegistryWrapper
    {
        public override IntermediateData CreateIntermediateData(IIntermediateDataManager intermediateDataManager)
        {
            return new TriangleMeshData(intermediateDataManager);
        }
    }
}