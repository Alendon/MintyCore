using System;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Render
{
    /// <summary>
    ///     Mesh contains all needed information's to render a object
    /// </summary>
    public struct Mesh : IDisposable
    {
        private byte _isStatic;

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

        public MemoryBuffer MemoryBuffer;

        public bool Equals(Mesh other)
        {
            return MemoryBuffer.Buffer.Handle == other.MemoryBuffer.Buffer.Handle;
        }

        public override bool Equals(object? obj)
        {
            return obj is Mesh other && Equals(other);
        }

        public static bool operator ==(Mesh left, Mesh right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Mesh left, Mesh right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            MemoryBuffer.Dispose();
            SubMeshIndexes.DecreaseRefCount();
        }
    }
}