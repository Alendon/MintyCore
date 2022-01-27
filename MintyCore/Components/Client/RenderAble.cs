using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Components.Client;

/// <summary>
///     Component to mark the entity as renderable and provide information's how to render
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

    private Mesh _mesh;

    /// <summary>
    ///     Get the <see cref="Mesh" />
    /// </summary>
    public Mesh GetMesh()
    {
        return _mesh;
    }

    /// <summary>
    ///     Set the id of the mesh
    /// </summary>
    public void SetMesh(Mesh mesh)
    {
        _mesh = mesh;
    }

    /// <summary>
    ///     Set the id of the mesh
    /// </summary>
    public void SetMesh(Identification staticMeshId)
    {
        _mesh = MeshHandler.GetStaticMesh(staticMeshId);
    }

    private UnmanagedArray<Material> _materials;

    /// <summary>
    /// Get a Material at a specific index. Returns index 0 if out of range
    /// </summary>
    public Material GetMaterialAtIndex(int index)
    {
        if (_materials.Length <= index || index < 0)
            return _materials[0];
        return _materials[index];
    }

    /// <summary>
    /// Set the materials for this component. The Reference Count of the current will be decreased and of the new one increased <seealso cref="Material"/>
    /// </summary>
    public void SetMaterials(UnmanagedArray<Material> materials)
    {
        _materials.DecreaseRefCount();
        _materials = materials;
        _materials.IncreaseRefCount();
    }

    /// <inheritdoc />
    public void Serialize(DataWriter writer, World world, Entity entity)
    {
        if (_mesh.IsStatic)
        {
            writer.Put((byte)1); //Put 1 to indicate that the mesh is static and "serializable"
            _mesh.StaticMeshId.Serialize(writer);
        }
        else
        {
            writer.Put((byte)0);
        }

        writer.Put(_materials.Length);
        foreach (var material in _materials) material.MaterialId.Serialize(writer);
    }

    /// <inheritdoc />
    public void Deserialize(DataReader reader, World world, Entity entity)
    {
        var serializableMesh = reader.GetByte();
        if (serializableMesh == 1)
        {
            var meshId = Identification.Deserialize(reader);
            _mesh = MeshHandler.GetStaticMesh(meshId);
        }

        var materialCount = reader.GetInt();
        _materials.DecreaseRefCount();
        _materials = new UnmanagedArray<Material>(materialCount);
        for (var i = 0; i < materialCount; i++)
        {
            var materialId = Identification.Deserialize(reader);
            _materials[i] = MaterialHandler.GetMaterial(materialId);
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