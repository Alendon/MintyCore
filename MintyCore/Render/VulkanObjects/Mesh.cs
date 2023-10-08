using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Render.VulkanObjects;

/// <summary>
///     Mesh contains all needed information's to render a object
/// </summary>
[PublicAPI]
public sealed class Mesh : VulkanObject
{
    /// <inheritdoc />
    public override int GetHashCode()
    {
        return MemoryBuffer.Buffer.Handle.GetHashCode();
    }

    /// <summary>
    ///     Specify whether a mesh is static or dynamic (changeable or not at runtime)
    /// </summary>
    public bool IsStatic => StaticMeshId != default;

    /// <summary>
    ///     Id of the mesh. Only set if its static
    /// </summary>
    public Identification StaticMeshId { get; internal init; }

    /// <summary>
    ///     The VertexCount of the <see cref="Mesh" />
    /// </summary>
    public uint VertexCount { get; internal set; }

    /// <summary>
    ///     The SubMeshIndices
    /// </summary>
    public (uint startIndex, uint length)[] SubMeshIndexes { get; internal set; }

    /// <summary>
    ///     The gpu buffer of the mesh
    /// </summary>
    public MemoryBuffer MemoryBuffer;

    public Mesh(IVulkanEngine vulkanEngine, IAllocationTracker allocationTracker, MemoryBuffer memoryBuffer,
        Identification staticMeshId, uint vertexCount, (uint startIndex, uint length)[] subMeshIndexes) : base(
        vulkanEngine, allocationTracker)
    {
        MemoryBuffer = memoryBuffer;
        StaticMeshId = staticMeshId;
        VertexCount = vertexCount;
        SubMeshIndexes = subMeshIndexes;
    }

    /// <summary>
    ///     Indicates whether this <see cref="Mesh" /> and another are equal
    /// </summary>
    /// <param name="other">The instance to compare</param>
    /// <returns>True if equal</returns>
    public bool Equals(Mesh other)
    {
        return MemoryBuffer.Buffer.Handle == other.MemoryBuffer.Buffer.Handle;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Mesh other && Equals(other);
    }

    /// <inheritdoc />
    protected override void ReleaseManagedResources()
    {
        MemoryBuffer.Dispose();
    }
}