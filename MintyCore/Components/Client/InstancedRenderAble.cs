using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Client
{
    public struct InstancedRenderAble : IComponent
    {
        public byte Dirty { get; set; }
        public Identification Identification => ComponentIDs.IndexedRenderAble;
        public Identification MaterialMeshCombination;
        
        public void PopulateWithDefaultValues()
        {
            
        }

        public void Serialize(DataWriter writer, World world, Entity entity)
        {  
            MaterialMeshCombination.Serialize(writer);
        }

        public void Deserialize(DataReader reader, World world, Entity entity)
        {
            MaterialMeshCombination = Identification.Deserialize(reader);
        }

        public void IncreaseRefCount()
        {
        }

        public void DecreaseRefCount()
        {
        }
    }
}