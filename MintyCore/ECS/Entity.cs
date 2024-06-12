using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     struct to identify a specific entity
/// </summary>
public readonly struct Entity : IEqualityComparer<Entity>, IEquatable<Entity>
{
    internal Entity(Identification archetypeId, uint id)
    {
        ArchetypeId = archetypeId;
        Id = id;
    }

    /// <summary>
    ///     The archetype of the <see cref="Entity" />
    /// </summary>
    public Identification ArchetypeId { get; }

    /// <summary>
    ///     The ID of the <see cref="Entity" />
    /// </summary>
    public uint Id { get; }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is not null && Equals((Entity)obj);
    }

    /// <inheritdoc />
    public bool Equals(Entity x, Entity y)
    {
        return x.Equals(y);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public bool Equals(Entity other)
    {
        return Id == other.Id && ArchetypeId.Equals(other.ArchetypeId);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(ArchetypeId, Id);
    }

    /// <inheritdoc />
    public int GetHashCode(Entity obj)
    {
        return obj.GetHashCode();
    }

    /// <summary>
    ///     Serialize the entity
    /// </summary>
    public void Serialize(DataWriter writer)
    {
        writer.Put(Id);
        writer.Put(ArchetypeId);
    }

    /// <summary>
    ///     Deserialize the entity
    /// </summary>
    /// <returns>True if deserialization was successful</returns>
    public static bool Deserialize(DataReader reader, out Entity entity)
    {
        if (!reader.TryGetUInt(out var id) || !reader.TryGetIdentification(out var archetypeId))
        {
            entity = default;
            return false;
        }

        entity = new Entity(archetypeId, id);
        return true;
    }

    /// <summary>
    ///     Operator to compare if to <see cref="Entity" /> are equal
    /// </summary>
    public static bool operator ==(Entity left, Entity right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Operator to compare if to <see cref="Entity" /> are not equal
    /// </summary>
    public static bool operator !=(Entity left, Entity right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{ArchetypeId.ToString()}:{Id}";
    }
}