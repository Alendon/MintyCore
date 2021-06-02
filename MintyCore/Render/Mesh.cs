using System;
using Veldrid;

namespace MintyCore.Render
{
    public class Mesh
    {
        public bool IsStatic { get; internal set; }

        public DeviceBuffer _vertexBuffer { get; internal set; }
        public uint _vertexCount { get; internal set; }
        public (uint startIndex, uint length)[] _submeshIndexes { get; internal set; }

        public void DrawMesh(CommandList commandList, uint bufferSlotIndex = 0)
        {
            commandList.SetVertexBuffer(bufferSlotIndex, _vertexBuffer);
            commandList.Draw(_vertexCount, 1, 0, 0);
        }

        public void DrawMesh(CommandList commandList, int meshGroupIndex, uint bufferSlotIndex = 0)
        {
            commandList.SetVertexBuffer(bufferSlotIndex, _vertexBuffer, _submeshIndexes[meshGroupIndex].startIndex);
            commandList.Draw(_submeshIndexes[meshGroupIndex].length, 1, 0, 0);
        }
    }
}