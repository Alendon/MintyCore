using System;
using JetBrains.Annotations;
using MintyCore.Graphics.Utils;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;

namespace MintyCore.Graphics.Managers;

/// <summary>
///   Manages the creation and deletion of meshes
/// </summary>
[PublicAPI]
public interface IMeshManager
{
    /// <summary>
    ///    Add a static mesh
    /// </summary>
    /// <param name="meshId"> Id of the mesh</param>
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

    /// <summary>
    ///   Clear all internal data
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Remove a static mesh
    /// </summary>
    void RemoveMesh(Identification objectId);
}