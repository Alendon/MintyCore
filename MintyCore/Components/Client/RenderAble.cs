using System.Runtime.InteropServices;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

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

	    /// <summary>
	    ///     Specify wether the used mesh is static or dynamic
	    /// </summary>
	    public byte StaticMesh;

        private MeshIdUnion _meshId;

        [StructLayout(LayoutKind.Explicit)]
        private struct MeshIdUnion
        {
            [FieldOffset(0)] internal Identification _staticMesh;

            [FieldOffset(0)] internal uint _dynamicMesh;
        }

        /// <summary>
        ///     Get the <see cref="Mesh" /> for the <see cref="Entity" />
        /// </summary>
        public Mesh GetMesh(Entity entity)
        {
            return StaticMesh != 0
                ? MeshHandler.GetStaticMesh(_meshId._staticMesh)
                : MeshHandler.GetDynamicMesh(entity, _meshId._dynamicMesh);
        }

        /// <summary>
        ///     Set the id of the mesh
        /// </summary>
        public void SetMesh(uint dynamicMeshId)
        {
            StaticMesh = 0;
            _meshId._dynamicMesh = dynamicMeshId;
        }

        /// <summary>
        ///     Set the id of the mesh
        /// </summary>
        public void SetMesh(Identification staticMeshId)
        {
            StaticMesh = 1;
            _meshId._staticMesh = staticMeshId;
        }

        /// <summary>
        ///     <see cref="Identification" /> of the used MaterialCollection
        /// </summary>
        public Identification MaterialCollectionId;

        /// <summary>
        ///     Get the MaterialCollection
        /// </summary>
        public Material[] GetMaterialCollection()
        {
            return MaterialHandler.GetMaterialCollection(MaterialCollectionId);
        }

        /// <inheritdoc />
        public void Serialize(DataWriter writer)
        {
        }

        /// <inheritdoc />
        public void Deserialize(DataReader reader)
        {
        }

        /// <summary>
        ///     Does nothing
        /// </summary>
        public void IncreaseRefCount()
        {
        }

        /// <summary>
        ///     Does nothing
        /// </summary>
        public void DecreaseRefCount()
        {
        }
    }
}