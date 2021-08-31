using System;
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
        
        private UnmanagedArray<GCHandle> _materials;

        public Material? GetMaterialAtIndex(int index)
        {
	        if (_materials.Length <= index || index < 0) throw new IndexOutOfRangeException();
	        return _materials[index].Target as Material;
        }

        public void SetMaterials(UnmanagedArray<GCHandle> materials)
        {
	        _materials.DecreaseRefCount();
	        _materials = materials;
	        _materials.IncreaseRefCount();
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