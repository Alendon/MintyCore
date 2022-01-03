using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using MintyCore.ECS;
using MintyCore.Registries;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;
using Silk.NET.Vulkan;
using Veldrid.Utilities;
using static MintyCore.Render.VulkanUtils;

namespace MintyCore.Render
{
    /// <summary>
    ///     Class to manage <see cref="Mesh" />
    /// </summary>
    public static class MeshHandler
    {
        private static readonly Dictionary<Identification, Mesh> _staticMeshes = new();

        private static readonly Dictionary<(World world, Entity entity), Mesh> _dynamicMeshPerEntity = new();

        private static Vertex[] _lastVertices = Array.Empty<Vertex>();

        internal static void Setup()
        {
            EntityManager.PreEntityDeleteEvent += OnEntityDelete;
        }

        private static void OnEntityDelete(World world, Entity entity)
        {
            if (!_dynamicMeshPerEntity.TryGetValue((world, entity), out var mesh)) return;
            _dynamicMeshPerEntity.Remove((world, entity));

            mesh.Dispose();
        }

        private static unsafe MemoryBuffer CreateMeshBuffer(Vertex[] vertices, uint vertexCount)
        {
            uint[] queueIndex = { VulkanEngine.QueueFamilyIndexes.GraphicsFamily!.Value };
            var memoryBuffer = MemoryBuffer.Create(BufferUsageFlags.BufferUsageVertexBufferBit,
                (ulong)(vertexCount * sizeof(Vertex)), SharingMode.Exclusive, queueIndex.AsSpan(),
                MemoryPropertyFlags.MemoryPropertyHostCoherentBit | MemoryPropertyFlags.MemoryPropertyHostVisibleBit);

            Vertex* bufferData;
            Assert(VulkanEngine.Vk.MapMemory(VulkanEngine.Device, memoryBuffer.Memory, 0, memoryBuffer.Size, 0,
                (void**)&bufferData));
            for (var index = 0; index < vertexCount; index++)
            {
                bufferData[index] = vertices[index];
            }

            VulkanEngine.Vk.UnmapMemory(VulkanEngine.Device, memoryBuffer.Memory);

            return memoryBuffer;
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
            foreach (var group in obj.MeshGroups) vertexCount += (uint)group.Faces.Length * 3u;

            //Reuse the last vertex array if possible to prevent memory allocations
            var vertices =
                _lastVertices.Length >= vertexCount ? _lastVertices : new Vertex[vertexCount];
            _lastVertices = vertices.Length >= _lastVertices.Length ? vertices : _lastVertices;

            uint index = 0;
            var iteration = 0;
            var meshIndices = new UnmanagedArray<(uint startIndex, uint length)>(obj.MeshGroups.Length);
            foreach (var group in obj.MeshGroups)
            {
                var startIndex = index;
                foreach (var face in group.Faces)
                {
                    vertices[index] = new Vertex(obj.Positions[face.Vertex0.PositionIndex - 1],
                        new Vector3(obj.TexCoords[face.Vertex0.TexCoordIndex - 1],1), obj.Normals[face.Vertex0.NormalIndex - 1],
                        obj.TexCoords[face.Vertex0.TexCoordIndex - 1]);

                    vertices[index + 1] = new Vertex(obj.Positions[face.Vertex1.PositionIndex - 1],
                        new Vector3(obj.TexCoords[face.Vertex1.TexCoordIndex - 1],1), obj.Normals[face.Vertex1.NormalIndex - 1],
                        obj.TexCoords[face.Vertex1.TexCoordIndex - 1]);

                    vertices[index + 2] = new Vertex(obj.Positions[face.Vertex2.PositionIndex - 1],
                        new Vector3(obj.TexCoords[face.Vertex2.TexCoordIndex - 1],1), obj.Normals[face.Vertex2.NormalIndex - 1],
                        obj.TexCoords[face.Vertex2.TexCoordIndex - 1]);
                    index += 3;
                }

                var length = index - startIndex;
                meshIndices[iteration].length = length;
                meshIndices[iteration].startIndex = startIndex;
                iteration++;
            }

            Mesh created = new Mesh()
            {
                IsStatic = true,
                MemoryBuffer = CreateMeshBuffer(vertices, vertexCount),
                VertexCount = vertexCount,
                StaticMeshId = meshId,
                SubMeshIndexes = meshIndices
            };
            _staticMeshes.Add(meshId, created);
            _lastVertices = vertices;
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
        /*public static GCHandle CreateDynamicMesh<TVertex>(TVertex[] vertices, (World, Entity) owner,
            params (uint startIndex, uint length)[] subMeshIndices) where TVertex : unmanaged, IVertex
        {
            var buffer = VulkanEngine.CreateBuffer((uint)(Marshal.SizeOf<TVertex>() * vertices.Length),
                BufferUsage.VertexBuffer);
            VulkanEngine.UpdateBuffer(buffer, vertices);

            Mesh mesh = new()
            {
                IsStatic = false,
                VertexCount = (uint)vertices.Length,
                VertexBuffer = buffer,
                SubMeshIndexes = subMeshIndices
            };

            var meshHandle = GCHandle.Alloc(mesh, GCHandleType.Normal);
            _dynamicMeshPerEntity.Add(owner, mesh);
            _dynamicMeshHandles.Add(mesh, meshHandle);

            return meshHandle;
        }*/

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
        /*public static GCHandle CreateDynamicMesh<TVertex>(IntPtr vertexData, uint vertexCount,
            (World, Entity) owner,
            params (uint startIndex, uint length)[] subMeshIndices) where TVertex : unmanaged, IVertex
        {
            var size = (uint)(Marshal.SizeOf<TVertex>() * vertexCount);
            var buffer = VulkanEngine.CreateBuffer(size, BufferUsage.VertexBuffer);
            VulkanEngine.UpdateBuffer(buffer, vertexData, size);

            Mesh mesh = new()
            {
                IsStatic = false,
                VertexCount = vertexCount,
                VertexBuffer = buffer,
                SubMeshIndexes = subMeshIndices
            };

            var meshHandle = GCHandle.Alloc(mesh, GCHandleType.Normal);
            _dynamicMeshPerEntity.Add(owner, mesh);
            _dynamicMeshHandles.Add(mesh, meshHandle);

            return meshHandle;
        }*/


        /// <summary>
        ///     Get static Mesh
        /// </summary>
        public static Mesh GetStaticMesh(Identification meshId)
        {
            return _staticMeshes[meshId];
        }

        internal static void Clear()
        {
            foreach (var mesh in _staticMeshes.Values)
            {
                mesh.Dispose();
            }

            foreach (var mesh in _dynamicMeshPerEntity.Values)
            {
                mesh.Dispose();
            }


            _staticMeshes.Clear();
            _dynamicMeshPerEntity.Clear();

            EntityManager.PreEntityDeleteEvent -= OnEntityDelete;
        }
    }
}