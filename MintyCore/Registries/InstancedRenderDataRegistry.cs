using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
/// <see cref="IRegistry"/> for instanced render data
/// </summary>
public class InstancedRenderDataRegistry : IRegistry
{
    /// <summary />
    public delegate void RegisterDelegate();

    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.InstancedRenderData;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => new[]
    {
        RegistryIDs.Mesh, RegistryIDs.Material
    };

    /// <inheritdoc />
    public void PreRegister()
    {
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
    }

    /// <inheritdoc />
    public void Clear()
    {
        InstancedRenderDataHandler.Clear();
        OnRegister = delegate { };
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
    }

    /// <summary />
    public static event RegisterDelegate OnRegister = delegate { };

    /// <summary>
    /// Register instanced render data
    /// </summary>
    /// <param name="modId">Id of the registering mod</param>
    /// <param name="stringIdentifier">String identifier of the render data</param>
    /// <param name="meshId">Id of the mesh used in the render data</param>
    /// <param name="materialIds">IDs of the materials used in the render data</param>
    /// <returns><see cref="Identification"/> of the created render data</returns>
    public static Identification RegisterInstancedRenderData(ushort modId, string stringIdentifier,
        Identification meshId, params Identification[] materialIds)
    {
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.InstancedRenderData, stringIdentifier);
        InstancedRenderDataHandler.AddMeshMaterial(id, MeshHandler.GetStaticMesh(meshId),
            materialIds.Select(MaterialHandler.GetMaterial).ToArray());
        return id;
    }
}