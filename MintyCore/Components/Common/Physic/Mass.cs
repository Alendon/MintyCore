using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using MintyCore.Registries;

namespace MintyCore.Components.Common.Physic;

/// <summary>
///     Store the mass of an entity
/// </summary>
[RegisterComponent("mass")]
public struct Mass : IComponent
{
    /// <inheritdoc />
    public bool Dirty { get; set; }

    /// <summary>
    ///     Get/Set the mass
    /// </summary>
    public float MassValue { get; set; }

    /// <summary>
    ///     Set the mass value to infinity
    /// </summary>
    public void SetInfiniteMass()
    {
        MassValue = 0;
    }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Mass" /> Component
    /// </summary>
    public Identification Identification => ComponentIDs.Mass;

    /// <inheritdoc />
    public bool Deserialize(DataReader reader, IWorld world, Entity entity)
    {
        if (!reader.TryGetFloat(out var result)) return false;

        MassValue = result;
        return true;
    }

    /// <inheritdoc />
    public void PopulateWithDefaultValues()
    {
        MassValue = 0;
    }

    /// <inheritdoc />
    public void Serialize(DataWriter writer, IWorld world, Entity entity)
    {
        writer.Put(MassValue);
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