using System;
using MintyCore.ECS;
using MintyCore.Graphics.Utils;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;

namespace MintyCore.Graphics.Managers;

public interface IMeshManager
{
    void Setup();
    void OnEntityDelete(IWorld world, Entity entity);
    void AddStaticMesh(Identification meshId);

    /// <summary>
    ///     Create a new mesh
    /// </summary>
    /// <param name="vertices">vertices of the mesh</param>
    /// <param name="vertexCount">vertex count to use</param>
    /// <returns>Newly created mesh</returns>
    Mesh CreateDynamicMesh(Span<Vertex> vertices, uint vertexCount);

    /// <summary>
    ///     Get Mesh
    /// </summary>
    Mesh GetStaticMesh(Identification meshId);

    void Clear();
    void RemoveMesh(Identification objectId);
}