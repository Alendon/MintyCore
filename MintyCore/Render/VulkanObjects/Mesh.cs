using System;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Render.VulkanObjects;

/// <summary>
///     Mesh contains all needed information's to render a object
/// </summary>
[PublicAPI]
public sealed class Mesh : VulkanObject, IEquatable<Mesh>
{
    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Buffer.Buffer.Handle.GetHashCode();
    }

    /// <summary>
    ///     Specify whether a mesh is static or dynamic (changeable or not at runtime)
    /// </summary>
    public bool IsStatic => StaticMeshId != default;

    /// <summary>
    ///     Id of the mesh. Only set if its static
    /// </summary>
    public Identification StaticMeshId { get; private init; }

    /// <summary>
    ///     The VertexCount of the <see cref="Mesh" />
    /// </summary>
    public uint VertexCount { get; private set; }

    /// <summary>
    ///     The SubMeshIndices
    /// </summary>
    public (uint startIndex, uint length)[] SubMeshIndexes { get; private set; }

    /// <summary>
    ///     The gpu buffer of the mesh
    /// </summary>
    public MemoryBuffer Buffer { get; private init; }

    /// <summary>
    ///  Create a new <see cref="Mesh" />
    /// </summary>
    /// <param name="vulkanEngine">The VulkanEngine where the mesh is allocated from</param>
    /// <param name="allocationHandler">The AllocationHandler to track the object lifetime</param>
    /// <param name="memoryBuffer">The gpu buffer of the mesh</param>
    /// <param name="staticMeshId">The id of the mesh. Only set if its static</param>
    /// <param name="vertexCount">The number of vertices the mesh has</param>
    /// <param name="subMeshIndexes">The ranges of the sub meshes</param>
    public Mesh(IVulkanEngine vulkanEngine, IAllocationHandler allocationHandler, MemoryBuffer memoryBuffer,
        Identification staticMeshId, uint vertexCount, (uint startIndex, uint length)[] subMeshIndexes) : base(
        vulkanEngine, allocationHandler)
    {
        Buffer = memoryBuffer;
        StaticMeshId = staticMeshId;
        VertexCount = vertexCount;
        SubMeshIndexes = subMeshIndexes;
    }

    /// <summary>
    ///     Indicates whether this <see cref="Mesh" /> and another are equal
    /// </summary>
    /// <param name="other">The instance to compare</param>
    /// <returns>True if equal</returns>
    public bool Equals(Mesh? other)
    {
        return Buffer.Buffer.Handle == other?.Buffer.Buffer.Handle;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Mesh other && Equals(other);
    }

    /// <inheritdoc />
    protected override void ReleaseManagedResources()
    {
        Buffer.Dispose();
    }
}