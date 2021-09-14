using System.Runtime.InteropServices;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Components.Client
{
    /// <summary>
    ///     Component to mark the entity as renderable and provide informations how to render
    /// </summary>
    public struct RenderAble : IComponent
    {
        /// <inheritdoc />
        public byte Dirty { get; set; }

        /// <inheritdoc />
        public void PopulateWithDefaultValues()
        {
        }

        /// <summary>
        ///     <see cref="Identification" /> of the <see cref="RenderAble" /> Component
        /// </summary>
        public Identification Identification => ComponentIDs.Renderable;

        private GCHandle _meshHandle;

        /// <summary>
        ///     Get the <see cref="Mesh" />
        /// </summary>
        public Mesh? GetMesh()
        {
            return _meshHandle.IsAllocated ? _meshHandle.Target as Mesh : null;
        }

        /// <summary>
        ///     Set the id of the mesh
        /// </summary>
        public void SetMesh(GCHandle meshHandle)
        {
            _meshHandle = meshHandle;
        }

        /// <summary>
        ///     Set the id of the mesh
        /// </summary>
        public void SetMesh(Identification staticMeshId)
        {
            _meshHandle = MeshHandler.GetStaticMeshHandle(staticMeshId);
        }

        /// <summary>
        ///     <see cref="Identification" /> of the used MaterialCollection
        /// </summary>
        public Identification MaterialCollectionId;

        private UnmanagedArray<GCHandle> _materials;

        /// <summary>
        /// Get a Material at a specific index. Returns index 0 if out of range
        /// </summary>
        public Material? GetMaterialAtIndex(int index)
        {
            if (_materials.Length <= index || index < 0)
                return _materials[0].Target as Material;
            return _materials[index].Target as Material;
        }

        /// <summary>
        /// Set the materials for this component. The Reference Count of the current will be decreased and of the new one increased <seealso cref="Material"/>
        /// </summary>
        public void SetMaterials(UnmanagedArray<GCHandle> materials)
        {
            _materials.DecreaseRefCount();
            _materials = materials;
            _materials.IncreaseRefCount();
        }

        /// <inheritdoc />
        public void Serialize(DataWriter writer)
        {
            if (_meshHandle.Target is Mesh mesh && mesh.IsStatic)
            {
                writer.Put((byte)1); //Put 1 to indicate that the mesh is static and "serializable"
                mesh.StaticMeshId.Serialize(writer);
            }
            else
            {
                writer.Put((byte)0);
            }

            writer.Put(_materials.Length);
            foreach (var materialHandle in _materials)
            {
                var material = (Material)materialHandle.Target;
                material?.MaterialId.Serialize(writer);
            }
        }

        /// <inheritdoc />
        public void Deserialize(DataReader reader)
        {
            var serializableMesh = reader.GetByte();
            if (serializableMesh == 1)
            {
                Identification meshId = default;
                meshId.Deserialize(reader);
                _meshHandle = MeshHandler.GetStaticMeshHandle(meshId);
            }

            var materialCount = reader.GetInt();
            _materials.DecreaseRefCount();
            _materials = new UnmanagedArray<GCHandle>(materialCount);
            for (int i = 0; i < materialCount; i++)
            {
                Identification materialId = default;
                materialId.Deserialize(reader);
                _materials[i] = MaterialHandler.GetMaterialHandle(materialId);
            }
        }

        /// <summary>
        ///     Increase the reference count of the used resources
        /// </summary>
        public void IncreaseRefCount()
        {
            _materials.IncreaseRefCount();
        }

        /// <summary>
        ///     Decrease the reference count of the used resources
        /// </summary>
        public void DecreaseRefCount()
        {
            _materials.DecreaseRefCount();
        }
    }
}