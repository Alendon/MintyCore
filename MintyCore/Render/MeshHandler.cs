﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MintyCore.ECS;
using MintyCore.Registries;
using MintyCore.Utils;
using Veldrid;
using Veldrid.Utilities;

namespace MintyCore.Render
{
	/// <summary>
	///     Class to manage <see cref="Mesh" />
	/// </summary>
	public static class MeshHandler
    {
        private static readonly Dictionary<Identification, Mesh> _staticMeshes = new();

        private static readonly Dictionary<(Entity entity, uint id), Mesh> _dynamicMeshes = new();

        private static DefaultVertex[] _lastVertices = Array.Empty<DefaultVertex>();

        internal static Mesh AddStaticMesh(Identification meshId)
        {
            var fileName = RegistryManager.GetResourceFileName(meshId);
            if (!fileName.Contains(".obj"))
                throw new ArgumentException(
                    "The mesh format is not supported (only Wavefront (OBJ) is supported at the current state)");

            if (!File.Exists(fileName)) throw new IOException("File to load does not exists");
            ObjFile obj;
            using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                obj = new ObjParser().Parse(stream);
            }

            uint vertexCount = 0;
            foreach (var group in obj.MeshGroups) vertexCount += (uint)@group.Faces.Length * 3u;

            //Reuse the last vertex array if possible to prevent memory allocations
            var vertices =
                _lastVertices.Length >= vertexCount ? _lastVertices : new DefaultVertex[vertexCount];
            _lastVertices = vertices.Length >= _lastVertices.Length ? vertices : _lastVertices;

            uint index = 0;
            var iteration = 0;
            var meshIndices = new (uint startIndex, uint length)[obj.MeshGroups.Length];
            foreach (var group in obj.MeshGroups)
            {
                var startIndex = index;
                foreach (var face in group.Faces)
                {
                    vertices[index] = new DefaultVertex(obj.Positions[face.Vertex0.PositionIndex - 1],
                        new Vector3(1), obj.Normals[face.Vertex0.NormalIndex - 1],
                        obj.TexCoords[face.Vertex0.TexCoordIndex - 1]);

                    vertices[index + 1] = new DefaultVertex(obj.Positions[face.Vertex1.PositionIndex - 1],
                        new Vector3(1), obj.Normals[face.Vertex1.NormalIndex - 1],
                        obj.TexCoords[face.Vertex1.TexCoordIndex - 1]);

                    vertices[index + 2] = new DefaultVertex(obj.Positions[face.Vertex2.PositionIndex - 1],
                        new Vector3(1), obj.Normals[face.Vertex2.NormalIndex - 1],
                        obj.TexCoords[face.Vertex2.TexCoordIndex - 1]);
                    index += 3;
                }

                var length = index - startIndex;
                meshIndices[iteration].length = length;
                meshIndices[iteration].startIndex = startIndex;
                iteration++;
            }

            var buffer = VulkanEngine.CreateBuffer((uint)(Marshal.SizeOf<DefaultVertex>() * vertexCount),
                BufferUsage.VertexBuffer);
            var span = new ReadOnlySpan<DefaultVertex>(vertices, 0, (int)vertexCount);
            VulkanEngine.UpdateBuffer(buffer, span);

            Mesh mesh = new()
            {
                IsStatic = true,
                VertexCount = vertexCount,
                VertexBuffer = buffer,
                SubMeshIndexes = meshIndices
            };

            _staticMeshes.Add(meshId, mesh);
            return mesh;
        }

        //TODO Add tracking if an entity gets destroyed to free the dynamic mesh
        //TODO Add possibility to link an dynamic mesh to multiple Entities

        /// <summary>
        ///     Create a Dynamic <see cref="Mesh" />
        /// </summary>
        /// <typeparam name="TVertex">
        ///     <see cref="IVertex" /> Type used for the <see cref="Mesh" />. Must be
        ///     <see langword="unmanaged" />
        /// </typeparam>
        /// <param name="vertices">Array of <typeparamref name="TVertex" /></param>
        /// <param name="owner"><see cref="Entity" /> Owner. Each dynamic mesh is related to one entity</param>
        /// <param name="subMeshIndices"></param>
        /// <returns></returns>
        public static (Mesh mesh, uint id) CreateDynamicMesh<TVertex>(TVertex[] vertices, Entity owner,
            params (uint startIndex, uint length)[] subMeshIndices) where TVertex : unmanaged, IVertex
        {
            var entityId = (owner, 0u);
            while (_dynamicMeshes.ContainsKey(entityId)) entityId.Item2++;

            var buffer = VulkanEngine.CreateBuffer((uint)(Marshal.SizeOf<TVertex>() * vertices.Length),
                BufferUsage.VertexBuffer);
            VulkanEngine.UpdateBuffer(buffer, vertices);

            Mesh mesh = new()
            {
                IsStatic = true,
                VertexCount = (uint)vertices.Length,
                VertexBuffer = buffer,
                SubMeshIndexes = subMeshIndices
            };
            _dynamicMeshes.Add(entityId, mesh);
            return (mesh, entityId.Item2);
        }

        /// <summary>
        ///     Create a Dynamic <see cref="Mesh" />
        /// </summary>
        /// <typeparam name="TVertex">
        ///     <see cref="IVertex" /> Type used for the <see cref="Mesh" />. Must be
        ///     <see langword="unmanaged" />
        /// </typeparam>
        /// <param name="vertexData">Pointer to Array of <typeparamref name="TVertex" /></param>
        /// <param name="vertexCount">Length of <typeparamref name="TVertex" /> Array</param>
        /// <param name="owner"><see cref="Entity" /> Owner. Each dynamic mesh is related to one entity</param>
        /// <param name="subMeshIndices"></param>
        /// <returns></returns>
        public static (Mesh mesh, uint id) CreateDynamicMesh<TVertex>(IntPtr vertexData, uint vertexCount, Entity owner,
            params (uint startIndex, uint length)[] subMeshIndices) where TVertex : unmanaged, IVertex
        {
            var entityId = (owner, 0u);
            while (_dynamicMeshes.ContainsKey(entityId)) entityId.Item2++;

            var size = (uint)(Marshal.SizeOf<TVertex>() * vertexCount);
            var buffer = VulkanEngine.CreateBuffer(size, BufferUsage.VertexBuffer);
            VulkanEngine.UpdateBuffer(buffer, vertexData, size);

            Mesh mesh = new()
            {
                IsStatic = true,
                VertexCount = vertexCount,
                VertexBuffer = buffer,
                SubMeshIndexes = subMeshIndices
            };
            _dynamicMeshes.Add(entityId, mesh);
            return (mesh, entityId.Item2);
        }

        /// <summary>
        ///     Free a dynamic Mesh
        /// </summary>
        public static void FreeMesh((Entity entity, uint id) entityId)
        {
            var mesh = _dynamicMeshes[entityId];
            mesh.VertexBuffer.Dispose();
            _dynamicMeshes.Remove(entityId);
        }

        /// <summary>
        ///     Free a dynamic Mesh
        /// </summary>
        public static void FreeMesh(Entity entity, uint id)
        {
            FreeMesh((entity, id));
        }

        /// <summary>
        ///     Get static Mesh
        /// </summary>
        public static Mesh GetStaticMesh(Identification meshId)
        {
            return _staticMeshes[meshId];
        }

        /// <summary>
        ///     Get dynamic Mesh
        /// </summary>
        public static Mesh GetDynamicMesh(Entity entity, uint id)
        {
            return _dynamicMeshes[(entity, id)];
        }

        internal static void Clear()
        {
            foreach (var mesh in _staticMeshes) mesh.Value.VertexBuffer.Dispose();

            foreach (var mesh in _dynamicMeshes) mesh.Value.VertexBuffer.Dispose();

            _staticMeshes.Clear();
            _dynamicMeshes.Clear();
        }
    }
}