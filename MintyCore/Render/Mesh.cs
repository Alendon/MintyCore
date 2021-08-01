using System;
using Veldrid;

namespace MintyCore.Render
{
	/// <summary>
	/// Mesh contains all needed informations to render a object
	/// </summary>
	public class Mesh
	{
		/// <summary>
		/// Specify wether a mesh is static or dynamic (changeable or not at runtime)
		/// </summary>
		public bool IsStatic { get; internal set; }

		/// <summary>
		/// The VertexBuffer of the <see cref="Mesh"/>
		/// </summary>
		public DeviceBuffer _vertexBuffer { get; internal set; }

		/// <summary>
		/// The VertexCount of the <see cref="Mesh"/>
		/// </summary>
		public uint _vertexCount { get; internal set; }

		/// <summary>
		/// The SubMeshIndices
		/// </summary>
		public (uint startIndex, uint length)[] _submeshIndexes { get; internal set; }

		/// <summary>
		/// Method to bind a mesh to the <paramref name="commandList"/>
		/// </summary>
		/// <param name="commandList"><see cref="CommandList"/> to bind to</param>
		/// <param name="bufferSlotIndex">Equvialant to the GLSL "gl_BaseInstance"</param>
		/// <param name="meshGroupIndex">SubMesh to bind</param>
		public void BindMesh(CommandList commandList, uint bufferSlotIndex = 0, uint meshGroupIndex = 0)
		{
			commandList.SetVertexBuffer(bufferSlotIndex, _vertexBuffer, _submeshIndexes[meshGroupIndex].startIndex);
		}

		/// <summary>
		/// Draw a mesh through the <paramref name="commandList"/>
		/// </summary>
		/// <param name="commandList"><see cref="CommandList"/> to draw with</param>
		/// <param name="meshGroupIndex">SubMesh to render</param>
		/// <param name="instanceStart">Equvialant to the GLSL "gl_BaseInstance</param>
		/// <param name="instanceCount"></param>
		public void DrawMesh(CommandList commandList, uint meshGroupIndex = 0, uint instanceStart = 0, uint instanceCount = 1)
		{
			commandList.Draw(_submeshIndexes[meshGroupIndex].length, instanceCount, _submeshIndexes[meshGroupIndex].startIndex, instanceStart);
		}

		/// <summary>
		/// Get the <see cref="IndirectDrawArguments"/>
		/// </summary>
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