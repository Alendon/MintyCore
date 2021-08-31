using System;
using Veldrid;

namespace MintyCore.Render
{
    /// <summary>
    ///     Mesh contains all needed information's to render a object
    /// </summary>
    public class Mesh : IDisposable
    {
        /// <summary>
        ///     Specify whether a mesh is static or dynamic (changeable or not at runtime)
        /// </summary>
        public bool IsStatic { get; internal set; }

        /// <summary>
        ///     The VertexBuffer of the <see cref="Mesh" />
        /// </summary>
        public DeviceBuffer? VertexBuffer { get; internal set; }

        /// <summary>
        ///     The VertexCount of the <see cref="Mesh" />
        /// </summary>
        public uint VertexCount { get; internal set; }

        /// <summary>
        ///     The SubMeshIndices
        /// </summary>
        public (uint startIndex, uint length)[]? SubMeshIndexes { get; internal set; }

        /// <summary>
        ///     Method to bind a mesh to the <paramref name="commandList" />
        /// </summary>
        /// <param name="commandList"><see cref="CommandList" /> to bind to</param>
        /// <param name="bufferSlotIndex">Equvialant to the GLSL "gl_BaseInstance"</param>
        /// <param name="meshGroupIndex">SubMesh to bind</param>
        public void BindMesh(CommandList commandList, uint bufferSlotIndex = 0, uint meshGroupIndex = 0)
        {
            SubMeshIndexes ??= new[] { ((uint)0, VertexCount) };
            commandList.SetVertexBuffer(bufferSlotIndex, VertexBuffer, SubMeshIndexes[meshGroupIndex].startIndex);
        }

        /// <summary>
        ///     Draw a mesh through the <paramref name="commandList" />
        /// </summary>
        /// <param name="commandList"><see cref="CommandList" /> to draw with</param>
        /// <param name="meshGroupIndex">SubMesh to render</param>
        /// <param name="instanceStart">Equvialant to the GLSL "gl_BaseInstance</param>
        /// <param name="instanceCount"></param>
        public void DrawMesh(CommandList commandList, uint meshGroupIndex = 0, uint instanceStart = 0,
            uint instanceCount = 1)
        {
            commandList.Draw(SubMeshIndexes[meshGroupIndex].length, instanceCount,
                SubMeshIndexes[meshGroupIndex].startIndex, instanceStart);
        }

        /// <summary>
        ///     Get the <see cref="IndirectDrawArguments" />
        /// </summary>
        public IndirectDrawArguments DrawMeshIndirect(uint meshGroupIndex = 0, uint instanceStart = 0,
            uint instanceCount = 1)
        {
            return new IndirectDrawArguments
            {
                InstanceCount = instanceCount,
                FirstInstance = instanceStart,
                FirstVertex = SubMeshIndexes[meshGroupIndex].startIndex,
                VertexCount = SubMeshIndexes[meshGroupIndex].length
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            VertexBuffer?.Dispose();
        }
    }
}