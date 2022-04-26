using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///     <see cref="IRegistry" /> for instanced render data
/// </summary>
[Registry("instanced_render_data")]
public class InstancedRenderDataRegistry : IRegistry
{
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
        OnPreRegister();
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
        OnPostRegister();
    }

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        if(Engine.HeadlessModeActive)
            return;
        InstancedRenderDataHandler.RemoveMeshMaterial(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <inheritdoc />
    public void Clear()
    {
        InstancedRenderDataHandler.Clear();
        ClearRegistryEvents();
    }

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    /// <summary>
    ///     Register instanced render data
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId">Id of the registering mod</param>
    /// <param name="stringIdentifier">String identifier of the render data</param>
    /// <param name="meshId">Id of the mesh used in the render data</param>
    /// <param name="materialIds">IDs of the materials used in the render data</param>
    /// <returns><see cref="Identification" /> of the created render data</returns>
    [Obsolete]
    public static Identification RegisterInstancedRenderData(ushort modId, string stringIdentifier,
        Identification meshId, params Identification[] materialIds)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.InstancedRenderData, stringIdentifier);
        if(Engine.HeadlessModeActive)
            return id;
        
        InstancedRenderDataHandler.AddMeshMaterial(id, MeshHandler.GetStaticMesh(meshId),
            materialIds.Select(MaterialHandler.GetMaterial).ToArray());
        return id;
    }
    
    /// <summary>
    /// Register a instanced render data
    /// Used by the source generator for <see cref="Registries.RegisterInstancedRenderDataAttribute"/>
    /// </summary>
    /// <param name="id">Id of the instanced render data</param>
    /// <param name="info">Info for creating</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterInstancedRenderData(Identification id, InstancedRenderDataInfo info)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        if(Engine.HeadlessModeActive)
            return;
        
        InstancedRenderDataHandler.AddMeshMaterial(id, MeshHandler.GetStaticMesh(info.MeshId),
            info.MaterialIds.Select(MaterialHandler.GetMaterial).ToArray());
    }
}

/// <summary>
/// Struct which contains the information of an instanced render data
/// </summary>
public struct InstancedRenderDataInfo
{
    /// <summary>
    /// Mesh used for instanced rendering
    /// </summary>
    public Identification MeshId;
    
    /// <summary>
    /// Materials used for instanced rendering
    /// </summary>
    public Identification[] MaterialIds;
}