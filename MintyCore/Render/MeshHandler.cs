using System;
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
        private static readonly Dictionary<Identification, GCHandle> _staticMeshHandles = new();

        private static readonly Dictionary<(World world, Entity entity), Mesh> _dynamicMeshPerEntity = new();
        private static readonly Dictionary<Mesh, GCHandle> _dynamicMeshHandles = new();

        private static DefaultVertex[] _lastVertices = Array.Empty<DefaultVertex>();

        internal static void Setup()
        {
            EntityManager.PreEntityDeleteEvent += OnEntityDelete;
        }

        private static void OnEntityDelete(World world, Entity entity)
        {
            if (!_dynamicMeshPerEntity.TryGetValue((world, entity), out var mesh)) return;
            _dynamicMeshPerEntity.Remove((world, entity));

            var handle = _dynamicMeshHandles[mesh];
            handle.Free();
            _dynamicMeshHandles.Remove(mesh);
            mesh.Dispose();
        }

        internal static void AddStaticMesh(Identification meshId)
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
            GCHandle meshHandle = GCHandle.Alloc(mesh, GCHandleType.Normal);

            _staticMeshes.Add(meshId, mesh);
            _staticMeshHandles.Add(meshId, meshHandle);
        }

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
        /// <returns>A <see cref="GCHandle"/> to access the <see cref="Mesh"/></returns>
        public static GCHandle CreateDynamicMesh<TVertex>(TVertex[] vertices, (World, Entity) owner,
            params (uint startIndex, uint length)[] subMeshIndices) where TVertex : unmanaged, IVertex
        {
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

            GCHandle meshHandle = GCHandle.Alloc(mesh, GCHandleType.Normal);
            _dynamicMeshPerEntity.Add(owner, mesh);
            _dynamicMeshHandles.Add(mesh, meshHandle);

            return meshHandle;
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
        public static GCHandle CreateDynamicMesh<TVertex>(IntPtr vertexData, uint vertexCount,
            (World, Entity) owner,
            params (uint startIndex, uint length)[] subMeshIndices) where TVertex : unmanaged, IVertex
        {
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

            GCHandle meshHandle = GCHandle.Alloc(mesh, GCHandleType.Normal);
            _dynamicMeshPerEntity.Add(owner, mesh);
            _dynamicMeshHandles.Add(mesh, meshHandle);

            return meshHandle;
        }


        /// <summary>
        ///     Get static Mesh
        /// </summary>
        public static Mesh GetStaticMesh(Identification meshId)
        {
            return _staticMeshes[meshId];
        }

        /// <summary>
        /// Get the <see cref="GCHandle"/> for a static <see cref="Mesh"/>
        /// </summary>
        public static GCHandle GetStaticMeshHandle(Identification meshId)
        {
            return _staticMeshHandles[meshId];
        }

        internal static void Clear()
        {
            foreach (var mesh in _staticMeshes.Values)
            {
                mesh.Dispose();
            }

            foreach (var meshHandle in _staticMeshHandles.Values)
            {
                meshHandle.Free();
            }

            foreach (var mesh in _dynamicMeshPerEntity.Values)
            {
                mesh.Dispose();
            }

            foreach (var meshHandle in _dynamicMeshHandles.Values)
            {
                meshHandle.Free();
            }

            _staticMeshes.Clear();
            _staticMeshHandles.Clear();
            _dynamicMeshPerEntity.Clear();
            _dynamicMeshHandles.Clear();

            EntityManager.PreEntityDeleteEvent -= OnEntityDelete;
        }
    }
}