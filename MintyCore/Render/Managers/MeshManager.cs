using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MintyCore.ECS;
using MintyCore.Lib.VeldridUtils;
using MintyCore.Modding;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.Managers;

/// <summary>
///     Class to manage <see cref="Mesh" />
/// </summary>
[Singleton<IMeshManager>(SingletonContextFlags.NoHeadless)]
public class MeshManager : IMeshManager
{
    private readonly Dictionary<Identification, Mesh> _staticMeshes = new();

    private readonly Dictionary<(IWorld world, Entity entity), Mesh> _dynamicMeshPerEntity = new();

    private Vertex[] _lastVertices = Array.Empty<Vertex>();

    private IRegistryManager RegistryManager => ModManager.RegistryManager;
    public required IModManager ModManager { init; private get; }

    public required IVulkanEngine VulkanEngine { init; private get; }
    public required IAllocationTracker AllocationTracker { init; private get; }
    public required IMemoryManager MemoryManager { init; private get; }

    public void Setup()
    {
        EntityManager.PreEntityDeleteEvent += OnEntityDelete;
    }

    public void OnEntityDelete(IWorld world, Entity entity)
    {
        if (!_dynamicMeshPerEntity.TryGetValue((world, entity), out var mesh)) return;
        _dynamicMeshPerEntity.Remove((world, entity));

        mesh.Dispose();
    }

    private unsafe MemoryBuffer CreateMeshBuffer(Span<Vertex> vertices, uint vertexCount)
    {
        uint[] queueIndex = { VulkanEngine.QueueFamilyIndexes.GraphicsFamily!.Value };

        //Create a staging buffer to store the data first
        var stagingBuffer = MemoryManager.CreateBuffer(
            BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferSrcBit,
            (ulong)(vertexCount * sizeof(Vertex)), queueIndex.AsSpan(),
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            true);

        var bufferData = (Vertex*)MemoryManager.Map(stagingBuffer.Memory);

        //Use the Span structure as this provide a pretty optimized way of coping data
        var bufferSpan = new Span<Vertex>(bufferData, (int)vertexCount);

        //Slice the source span as it could be longer than our destination
        vertices[..(int)vertexCount].CopyTo(bufferSpan);

        MemoryManager.UnMap(stagingBuffer.Memory);

        //Create the actual gpu buffer
        var buffer = MemoryManager.CreateBuffer(BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit,
            (ulong)(vertexCount * sizeof(Vertex)), queueIndex.AsSpan(),
            MemoryPropertyFlags.DeviceLocalBit,
            false);

        //Copy the data from the staging to the gpu buffer
        var commandBuffer = VulkanEngine.GetSingleTimeCommandBuffer();

        BufferCopy region = new()
        {
            Size = buffer.Size,
            DstOffset = 0,
            SrcOffset = 0
        };
        VulkanEngine.Vk.CmdCopyBuffer(commandBuffer, stagingBuffer.Buffer, buffer.Buffer, 1, region);

        VulkanEngine.ExecuteSingleTimeCommandBuffer(commandBuffer);

        stagingBuffer.Dispose();

        return buffer;
    }

    public void AddStaticMesh(Identification meshId)
    {
        var fileName = RegistryManager.GetResourceFileName(meshId);
        Logger.AssertAndThrow(fileName.EndsWith(".obj"),
            "The mesh format is not supported (only Wavefront (OBJ) is supported at the current state)", "Render");

        ObjFile obj;
        using (var stream = ModManager.GetResourceFileStream(meshId))
        {
            obj = new ObjParser().Parse(stream);
        }

        var vertexCount =
            obj.MeshGroups.Aggregate<ObjFile.MeshGroup, uint>(0,
                (current, group) => current + (uint)group.Faces.Length * 3u);

        //Reuse the last vertex array if possible to prevent memory allocations
        var vertices =
            _lastVertices.Length >= vertexCount ? _lastVertices : new Vertex[vertexCount];
        _lastVertices = vertices.Length >= _lastVertices.Length ? vertices : _lastVertices;

        uint index = 0;
        var iteration = 0;
        var meshIndices = new (uint startIndex, uint length)[(obj.MeshGroups.Length)];
        foreach (var group in obj.MeshGroups)
        {
            var startIndex = index;
            foreach (var face in group.Faces)
            {
                vertices[index] = new Vertex(obj.Positions[face.Vertex0.PositionIndex - 1],
                    new Vector3(obj.TexCoords[face.Vertex0.TexCoordIndex - 1], 1),
                    obj.Normals[face.Vertex0.NormalIndex - 1],
                    obj.TexCoords[face.Vertex0.TexCoordIndex - 1]);

                vertices[index + 1] = new Vertex(obj.Positions[face.Vertex1.PositionIndex - 1],
                    new Vector3(obj.TexCoords[face.Vertex1.TexCoordIndex - 1], 1),
                    obj.Normals[face.Vertex1.NormalIndex - 1],
                    obj.TexCoords[face.Vertex1.TexCoordIndex - 1]);

                vertices[index + 2] = new Vertex(obj.Positions[face.Vertex2.PositionIndex - 1],
                    new Vector3(obj.TexCoords[face.Vertex2.TexCoordIndex - 1], 1),
                    obj.Normals[face.Vertex2.NormalIndex - 1],
                    obj.TexCoords[face.Vertex2.TexCoordIndex - 1]);
                index += 3;
            }

            var length = index - startIndex;
            meshIndices[iteration].length = length;
            meshIndices[iteration].startIndex = startIndex;
            iteration++;
        }

        var created = new Mesh(VulkanEngine, AllocationTracker, CreateMeshBuffer(vertices, vertexCount), meshId,
            vertexCount, meshIndices);

        _staticMeshes.Add(meshId, created);
        _lastVertices = vertices;
    }

    /// <summary>
    ///     Create a new mesh
    /// </summary>
    /// <param name="vertices">vertices of the mesh</param>
    /// <param name="vertexCount">vertex count to use</param>
    /// <returns>Newly created mesh</returns>
    public Mesh CreateDynamicMesh(Span<Vertex> vertices, uint vertexCount)
    {
        return new Mesh(VulkanEngine, AllocationTracker, CreateMeshBuffer(vertices, vertexCount),
            Identification.Invalid, vertexCount, new[] { (0u, vertexCount) });
    }


    /// <summary>
    ///     Get Mesh
    /// </summary>
    public Mesh GetStaticMesh(Identification meshId)
    {
        return _staticMeshes[meshId];
    }

    public void Clear()
    {
        foreach (var mesh in _staticMeshes.Values) mesh.Dispose();

        foreach (var mesh in _dynamicMeshPerEntity.Values) mesh.Dispose();


        _staticMeshes.Clear();
        _dynamicMeshPerEntity.Clear();

        EntityManager.PreEntityDeleteEvent -= OnEntityDelete;
    }

    public void RemoveMesh(Identification objectId)
    {
        if (_staticMeshes.Remove(objectId, out var mesh)) mesh.Dispose();
    }
}