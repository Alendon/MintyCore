using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Client;

/// <summary>
///     Component to render the entity instanced
/// </summary>
public struct InstancedRenderAble : IComponent
{
    /// <inheritdoc />
    public byte Dirty { get; set; }

    /// <inheritdoc />
    public Identification Identification => ComponentIDs.InstancedRenderAble;

    /// <summary>
    ///     The material mesh combination to use for rendering
    /// </summary>
    public Identification MaterialMeshCombination;

    /// <inheritdoc />
    public void PopulateWithDefaultValues()
    {
    }

    /// <inheritdoc />
    public void Serialize(DataWriter writer, World world, Entity entity)
    {
        MaterialMeshCombination.Serialize(writer);
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader, World world, Entity entity)
    {
        if (!Identification.Deserialize(reader, out var result)) return false;
        MaterialMeshCombination = result;
        return true;
    }

    /// <inheritdoc />
    public void IncreaseRefCount()
    {
    }

    /// <inheritdoc />
    public void DecreaseRefCount()
    {
    }
}