using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Mesh" />
/// </summary>
public class MeshRegistry : IRegistry
{
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
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Meshes", LogImportance.INFO, "Registry");
        ClearRegistryEvents();
        MeshHandler.Clear();
    }


    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Mesh;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    /// <summary>
    ///     Register a mesh
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Mesh" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Mesh" /></param>
    /// <param name="meshName">The resource name of the <see cref="Mesh" /></param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="Mesh" /></returns>
    public static Identification RegisterMesh(ushort modId, string stringIdentifier, string meshName)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Mesh, stringIdentifier, meshName);

        MeshHandler.AddStaticMesh(id);

        return id;
    }
}