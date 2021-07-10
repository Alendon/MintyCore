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

		public void BindMesh(CommandList commandList, uint bufferSlotIndex = 0, uint meshGroupIndex = 0)
		{
			commandList.SetVertexBuffer(bufferSlotIndex, _vertexBuffer, _submeshIndexes[meshGroupIndex].startIndex);
		}

		public void DrawMesh(CommandList commandList, uint meshGroupIndex = 0, uint instanceStart = 0, uint instanceCount = 1)
		{
			commandList.Draw(_submeshIndexes[meshGroupIndex].length, instanceCount, _submeshIndexes[meshGroupIndex].startIndex, instanceStart);
		}

		public IndirectDrawArguments DrawMeshIndirect(uint meshGroupIndex = 0, uint instanceStart = 0, uint instanceCount = 1)
		{
			return new() 
			{ 
				InstanceCount = instanceCount, 
				FirstInstance = instanceStart, 
				FirstVertex = _submeshIndexes[meshGroupIndex].startIndex, 
				VertexCount = _submeshIndexes[meshGroupIndex].length 
			};
		}
	}
}