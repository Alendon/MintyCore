using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries;

public class InstancedRenderDataRegistry : IRegistry
{
    /// <summary />
    public delegate void RegisterDelegate();

    public ushort RegistryId => RegistryIDs.InstancedRenderData;

    public IEnumerable<ushort> RequiredRegistries => new[]
    {
        RegistryIDs.Mesh, RegistryIDs.Material
    };

    public void PreRegister()
    {
    }

    public void Register()
    {
        OnRegister();
    }

    public void PostRegister()
    {
    }

    public void Clear()
    {
        InstancedRenderDataHandler.Clear();
        OnRegister = delegate { };
    }
        
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
    }

    /// <summary />
    public static event RegisterDelegate OnRegister = delegate { };

    public static Identification RegisterInstancedRenderData(ushort modId, string stringIdentifier,
        Identification meshId, params Identification[] materialIds)
    {
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.InstancedRenderData, stringIdentifier);
        InstancedRenderDataHandler.AddMeshMaterial(id, MeshHandler.GetStaticMesh(meshId),
            materialIds.Select(MaterialHandler.GetMaterial).ToArray());
        return id;
    }
}