using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Components.Client;

/// <summary>
///     Component to mark the entity as renderable and provide information's how to render
/// </summary>
[RegisterComponent("renderable")]
public struct RenderAble : IComponent
{
    /// <inheritdoc />
    public bool Dirty { get; set; }

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
    ///     Get a Material at a specific index. Returns index 0 if out of range
    /// </summary>
    public Material GetMaterialAtIndex(int index)
    {
        if (_materials.Length <= index || index < 0)
            return _materials[0];
        return _materials[index];
    }

    /// <summary>
    ///     Set the materials for this component. The Reference Count of the current will be decreased and of the new one
    ///     increased <seealso cref="Material" />
    /// </summary>
    public void SetMaterials(UnmanagedArray<Material> materials)
    {
        _materials.DecreaseRefCount();
        _materials = materials;
        _materials.IncreaseRefCount();
    }

    /// <inheritdoc />
    public void Serialize(DataWriter writer, IWorld world, Entity entity)
    {
        if (_mesh.IsStatic)
        {
            writer.Put(true);
            _mesh.StaticMeshId.Serialize(writer);
        }
        else
        {
            writer.Put(false);
        }

        writer.Put(_materials.Length);
        foreach (var material in _materials) material.MaterialId.Serialize(writer);
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader, IWorld world, Entity entity)
    {
        if (!reader.TryGetBool(out var serializableMesh)) return false;

        if (serializableMesh)
        {
            if (!Identification.Deserialize(reader, out var meshId)) return false;

            _mesh = MeshHandler.GetStaticMesh(meshId);
        }

        if (!reader.TryGetInt(out var materialCount)) return false;

        _materials.DecreaseRefCount();
        _materials = new UnmanagedArray<Material>(materialCount);
        for (var i = 0; i < materialCount; i++)
        {
            if (!Identification.Deserialize(reader, out var materialId)) return false;

            _materials[i] = MaterialHandler.GetMaterial(materialId);
        }

        return true;
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