using System;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Render;

/// <summary>
///     Mesh contains all needed information's to render a object
/// </summary>
public struct Mesh : IDisposable
{
    /// <inheritdoc />
    public override int GetHashCode()
    {
        return MemoryBuffer.Buffer.Handle.GetHashCode();
    }

    private readonly byte _isStatic;

    /// <summary>
    ///     Specify whether a mesh is static or dynamic (changeable or not at runtime)
    /// </summary>
    public bool IsStatic
    {
        get => _isStatic != 0;
        internal init => _isStatic = value ? (byte)1 : (byte)0;
    }

    /// <summary>
    /// Id of the mesh. Only set if its static
    /// </summary>
    public Identification StaticMeshId { get; internal init; }

    /// <summary>
    ///     The VertexCount of the <see cref="Mesh" />
    /// </summary>
    public uint VertexCount { get; internal set; }

    /// <summary>
    ///     The SubMeshIndices
    /// </summary>
    public UnmanagedArray<(uint startIndex, uint length)> SubMeshIndexes { get; internal set; }

    /// <summary>
    /// The gpu buffer of the mesh
    /// </summary>
    public MemoryBuffer MemoryBuffer;

    
    /// <summary>
    /// Indicates whether this <see cref="Mesh"/> and another are equal
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
    public void Dispose()
    {
        MemoryBuffer.Dispose();
        SubMeshIndexes.DecreaseRefCount();
    }
}