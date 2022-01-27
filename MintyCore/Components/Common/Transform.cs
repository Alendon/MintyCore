using System.Numerics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common;

/// <summary>
///     Component to store the transform value of an entity (as "T:Ara3d.Matrix4x4")
/// </summary>
public struct Transform : IComponent
{
    /// <summary>
    ///     The value of an entities transform
    /// </summary>
    public Matrix4x4 Value;

    /// <inheritdoc />
    public byte Dirty { get; set; }

    /// <inheritdoc />
    public Identification Identification => ComponentIDs.Transform;

    /// <inheritdoc />
    public void DecreaseRefCount()
    {
    }

    /// <inheritdoc />
    public void Deserialize(DataReader reader, World world, Entity entity)
    {
        Value = reader.GetMatrix4X4();
    }

    /// <inheritdoc />
    public void IncreaseRefCount()
    {
    }

    /// <inheritdoc />
    public void PopulateWithDefaultValues()
    {
        Value = Matrix4x4.Identity;
    }

    /// <inheritdoc />
    public void Serialize(DataWriter writer, World world, Entity entity)
    {
        writer.Put(Value);
    }
}