using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using System.Runtime.InteropServices;

namespace MintyCore.Components.Client
{
    /// <summary>
    /// Component to mark the entity as renderable and provide informations how to render
    /// </summary>
    public struct Renderable : IComponent
    {
        /// <inheritdoc/>
        public void Dispose()
        {
            
        }

        /// <inheritdoc/>
        public byte Dirty { get; set; }
        /// <inheritdoc/>
        public void PopulateWithDefaultValues()
        {
            
        }

        /// <inheritdoc/>
        public Identification Identification => ComponentIDs.Renderable;

        /// <summary>
        /// Specify wether the used mesh is static or dynamic
        /// </summary>
        public byte _staticMesh;
        private MeshIdUnion _meshId;

        [StructLayout(LayoutKind.Explicit)]
        struct MeshIdUnion
        {
            [FieldOffset(0)]
            internal Identification _staticMesh;

            [FieldOffset(0)]
            internal uint _dynamicMesh;
        }

        /// <summary>
        /// Get the <see cref="Mesh"/> for the <see cref="Entity"/>
        /// </summary>
        public Mesh GetMesh(Entity entity)
        {
            return _staticMesh != 0
                ? MeshHandler.GetStaticMesh(_meshId._staticMesh)
                : MeshHandler.GetDynamicMesh(entity, _meshId._dynamicMesh);
        }

        /// <summary>
        /// Set the id of the mesh
        /// </summary>
        public void SetMesh(uint dynamicMeshId)
		{
            _staticMesh = 0;
            _meshId._dynamicMesh = dynamicMeshId;
		}

        /// <summary>
        /// Set the id of the mesh
        /// </summary>
        public void SetMesh(Identification staticMeshId)
		{
            _staticMesh = 1;
            _meshId._staticMesh = staticMeshId;
		}

        /// <summary>
        /// <see cref="Identification"/> of the used MaterialCollection
        /// </summary>
        public Identification _materialCollectionId;

        /// <summary>
        /// Get the MaterialCollection
        /// </summary>
        public Material[] GetMaterialCollection()
        {
            return MaterialHandler.GetMaterialCollection(_materialCollectionId);
        }

        /// <inheritdoc/>
        public void Serialize(DataWriter writer)
        {
            
        }

        /// <inheritdoc/>
        public void Deserialize(DataReader reader)
        {
            
        }
    }
}