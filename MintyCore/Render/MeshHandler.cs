using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Ara3D;
using MintyCore.ECS;
using MintyCore.Registries;
using MintyCore.Utils;
using Veldrid;
using Veldrid.Utilities;

namespace MintyCore.Render
{
    public static class MeshHandler
    {
        private static ObjParser _parser = new();

        private static Dictionary<Identification, Mesh> _staticMeshes = new();

        private static Dictionary<(Entity entity, uint id), Mesh> _dynamicMeshes = new();

        private static DefaultVertex[] _lastVertices = Array.Empty<DefaultVertex>();

        internal static Mesh AddStaticMesh(Identification meshId)
        {
            string fileName = RegistryManager.GetResourceFileName(meshId);
            if (!fileName.Contains(".obj"))
            {
                throw new ArgumentException(
                    "The mesh format is not supported (only Wavefront (OBJ) is supported at the current state)");
            }

            if (!File.Exists(fileName))
            {
                throw new IOException("File to load does not exists");
            }

            var obj = _parser.Parse(File.Open(fileName, FileMode.Open, FileAccess.Read));

            uint vertexCount = 0;
            foreach (var group in obj.MeshGroups)
            {
                vertexCount += (uint)group.Faces.Length * 3u;
            }

            //Reuse the last vertex array if possible to prevent memory allocations
            DefaultVertex[] vertices =
                _lastVertices.Length >= vertexCount ? _lastVertices : new DefaultVertex[vertexCount];
            _lastVertices = vertices.Length >= _lastVertices.Length ? vertices : _lastVertices;

            uint index = 0;
            int iteration = 0;
            (uint startIndex, uint length)[] meshIndices = new (uint startIndex, uint length)[obj.MeshGroups.Length];
            foreach (var group in obj.MeshGroups)
            {
                uint startIndex = index;
                foreach (var face in group.Faces)
                {
                    vertices[index] = new DefaultVertex(obj.Positions[face.Vertex0.PositionIndex].ToAra3DVector(),
                        new Vector3(100), obj.Normals[face.Vertex0.NormalIndex].ToAra3DVector(),
                        obj.TexCoords[face.Vertex0.TexCoordIndex].ToAra2DVector());

                    vertices[index + 1] = new DefaultVertex(obj.Positions[face.Vertex0.PositionIndex].ToAra3DVector(),
                        new Vector3(100), obj.Normals[face.Vertex0.NormalIndex].ToAra3DVector(),
                        obj.TexCoords[face.Vertex0.TexCoordIndex].ToAra2DVector());

                    vertices[index + 2] = new DefaultVertex(obj.Positions[face.Vertex0.PositionIndex].ToAra3DVector(),
                        new Vector3(100), obj.Normals[face.Vertex0.NormalIndex].ToAra3DVector(),
                        obj.TexCoords[face.Vertex0.TexCoordIndex].ToAra2DVector());
                    index += 3;
                }

                uint length = index - startIndex;
                meshIndices[iteration].length = length;
                meshIndices[iteration].startIndex = startIndex;
                iteration++;
            }

            var buffer = MintyCore.VulkanEngine.CreateBuffer((uint) (Marshal.SizeOf<DefaultVertex>() * vertexCount),
                BufferUsage.VertexBuffer);
            MintyCore.VulkanEngine.UpdateBuffer(buffer, vertices);

            Mesh mesh = new()
            {
                IsStatic = true, _vertexCount = vertexCount, _vertexBuffer = buffer, _submeshIndexes = meshIndices
            };

            _staticMeshes.Add(meshId, mesh);
            return mesh;
        }

        public static (Mesh mesh, uint id) CreateDynamicMesh<TVertex>(TVertex[] vertices, Entity owner,
            params (uint startIndex, uint length)[] subMeshIndices) where TVertex : unmanaged, IVertex
        {
            var entityId = (owner, 0u);
            while (_dynamicMeshes.ContainsKey(entityId))
            {
                entityId.Item2++;
            }

            var buffer = MintyCore.VulkanEngine.CreateBuffer((uint) (Marshal.SizeOf<TVertex>() * vertices.Length),
                BufferUsage.VertexBuffer);
            MintyCore.VulkanEngine.UpdateBuffer(buffer, vertices);

            Mesh mesh = new()
            {
                IsStatic = true, _vertexCount = (uint)vertices.Length, _vertexBuffer = buffer,
                _submeshIndexes = subMeshIndices
            };
            _dynamicMeshes.Add(entityId, mesh);
            return (mesh, entityId.Item2);
        }

        public static (Mesh mesh, uint id) CreateDynamicMesh<TVertex>(IntPtr vertexData, uint vertexCount, Entity owner,
            params (uint startIndex, uint length)[] subMeshIndices) where TVertex : unmanaged, IVertex
        {
            var entityId = (owner, 0u);
            while (_dynamicMeshes.ContainsKey(entityId))
            {
                entityId.Item2++;
            }

            var size = (uint) (Marshal.SizeOf<TVertex>() * vertexCount);
            var buffer = MintyCore.VulkanEngine.CreateBuffer(size, BufferUsage.VertexBuffer);
            MintyCore.VulkanEngine.UpdateBuffer(buffer, vertexData, size);

            Mesh mesh = new()
            {
                IsStatic = true, _vertexCount = vertexCount, _vertexBuffer = buffer, _submeshIndexes = subMeshIndices
            };
            _dynamicMeshes.Add(entityId, mesh);
            return (mesh, entityId.Item2);
        }

        public static void FreeMesh(Identification id)
        {
            var mesh = _staticMeshes[id];
            if (!mesh._vertexBuffer.IsDisposed)
            {
                mesh._vertexBuffer.Dispose();
            }

            _staticMeshes.Remove(id);
        }

        public static void FreeMesh((Entity entity, uint id) entityId)
        {
            var mesh = _dynamicMeshes[entityId];
            if (!mesh._vertexBuffer.IsDisposed)
            {
                mesh._vertexBuffer.Dispose();
            }

            _dynamicMeshes.Remove(entityId);
        }
        public static void FreeMesh(Entity entity, uint id)
        {
            FreeMesh((entity,id));
        }
    }
}