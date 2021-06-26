using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Components.Client
{
    public struct Renderable : IComponent
    {
        public void Dispose()
        {
            
        }

        public byte Dirty { get; }
        public void PopulateWithDefaultValues()
        {
            
        }

        public Identification Identification => ComponentIDs.Renderable;

        public byte _staticMesh;
        public Identification _staticMeshId;
        public uint _dynamicMeshId;

        public Mesh GetMesh(Entity entity)
        {
            return _staticMesh != 0
                ? MeshHandler.GetStaticMesh(_staticMeshId)
                : MeshHandler.GetDynamicMesh(entity, _dynamicMeshId);
        }

        public Identification _materialCollectionId;

        public Material[] GetMaterial()
        {
            return MaterialHandler.GetMaterialCollection(_materialCollectionId);
        }
        
        public void Serialize(DataWriter writer)
        {
            
        }

        public void Deserialize(DataReader reader)
        {
            
        }
    }
}