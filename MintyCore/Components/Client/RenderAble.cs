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

	    private GCHandle _meshHandle;
	    
        /// <summary>
        ///     Get the <see cref="Mesh" />
        /// </summary>
        public Mesh? GetMesh()
        {
	        return _meshHandle.Target as Mesh;
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