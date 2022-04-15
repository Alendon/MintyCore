using System.Numerics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common;

/// <summary>
///     Holds the position of an entity
/// </summary>
public struct Position : IComponent
{
    /// <inheritdoc />
    public bool Dirty { get; set; }

    /// <summary>
    ///     Value of the position
    /// </summary>
    public Vector3 Value;

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Position" /> Component
    /// </summary>
    public Identification Identification => ComponentIDs.Position;

    /// <inheritdoc />
    public bool Deserialize(DataReader reader, IWorld world, Entity entity)
    {
        if (!reader.TryGetVector3(out var result)) return false;

        Value = result;
        return true;
    }

    /// <inheritdoc />
    public void PopulateWithDefaultValues()
    {
        Value = Vector3.Zero;
    }

    /// <inheritdoc />
    public void Serialize(DataWriter writer, IWorld world, Entity entity)
    {
        writer.Put(Value);
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