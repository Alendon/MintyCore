using System;
using System.Runtime.CompilerServices;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
/// Base class for custom component serializers
/// </summary>
public abstract class ComponentSerializer
{
    /// <summary>
    /// Serialize the component data
    /// </summary>
    /// <param name="componentPtr"> Pointer to the component data </param>
    /// <param name="writer"> The DataWriter to serialize with </param>
    /// <param name="world"> The world the component lives in </param>
    /// <param name="entity"> The entity the component belongs to </param>
    public abstract void Serialize(IntPtr componentPtr, DataWriter writer, IWorld world, Entity entity);

    /// <summary>
    /// Deserialize the component data
    /// </summary>
    /// <param name="componentPtr"> Pointer to the component data </param>
    /// <param name="reader"> The DataReader to deserialize with </param>
    /// <param name="world"> The world the component lives in </param>
    /// <param name="entity"> The entity the component belongs to </param>
    public abstract bool Deserialize(IntPtr componentPtr, DataReader reader, IWorld world, Entity entity);

    public abstract Identification ComponentId { get; }
}

/// <summary>
/// Base class for custom component serializers
/// </summary>
/// <typeparam name="TComponent"> Type of the component to serialize </typeparam>
public abstract unsafe class ComponentSerializer<TComponent> : ComponentSerializer
    where TComponent : unmanaged, IComponent
{
    /// <summary>
    ///   Serialize the data of the component
    /// </summary>
    /// <param name="component"> The component to serialize </param>
    /// <param name="writer"> The DataWriter to serialize with </param>
    /// <param name="world"> The world the component lives in </param>
    /// <param name="entity"> The entity the component belongs to </param>
    public abstract void Serialize(ref TComponent component, DataWriter writer, IWorld world, Entity entity);

    /// <summary>
    ///  Deserialize the data of the component
    /// </summary>
    /// <param name="component"> The component to deserialize </param>
    /// <param name="reader"> The DataReader to deserialize with </param>
    /// <param name="world"> The world the component lives in </param>
    /// <param name="entity"> The entity the component belongs to </param>
    public abstract bool Deserialize(ref TComponent component, DataReader reader, IWorld world, Entity entity);

    /// <inheritdoc />
    public sealed override void Serialize(IntPtr componentPtr, DataWriter writer, IWorld world, Entity entity)
    {
        Serialize(ref Unsafe.AsRef<TComponent>(componentPtr.ToPointer()), writer, world, entity);
    }

    /// <inheritdoc />
    public sealed override bool Deserialize(IntPtr componentPtr, DataReader reader, IWorld world, Entity entity)
    {
        return Deserialize(ref Unsafe.AsRef<TComponent>(componentPtr.ToPointer()), reader, world, entity);
    }

    /// <inheritdoc />
    public override Identification ComponentId => ((TComponent)default).Identification;
}