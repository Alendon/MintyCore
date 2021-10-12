using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries
{
	/// <summary>
	///     The <see cref="IRegistry" /> class for all <see cref="Mesh" />
	/// </summary>
	public class MeshRegistry : IRegistry
    {
	    /// <summary />
	    public delegate void RegisterDelegate();

	    /// <inheritdoc />
	    public void PreRegister()
        {
	        MeshHandler.Setup();
        }

	    /// <inheritdoc />
	    public void Register()
        {
            Logger.WriteLog("Registering Meshes", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

	    /// <inheritdoc />
	    public void PostRegister()
        {
        }

	    /// <inheritdoc />
	    public void Clear()
        {
	        Logger.WriteLog("Clearing Meshes", LogImportance.INFO, "Registry");
            OnRegister = delegate { };
            MeshHandler.Clear();
        }

	    /// <inheritdoc />
	    public ushort RegistryId => RegistryIDs.Mesh;

	    /// <inheritdoc />
	    public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();

	    /// <summary />
	    public static event RegisterDelegate OnRegister = delegate { };

	    /// <summary>
	    ///     Register a mesh
	    /// </summary>
	    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Mesh" /></param>
	    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Mesh" /></param>
	    /// <param name="meshName">The resource name of the <see cref="Mesh" /></param>
	    /// <returns>Generated <see cref="Identification" /> for <see cref="Mesh" /></returns>
	    public static Identification RegisterMesh(ushort modId, string stringIdentifier, string meshName)
        {
            var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Mesh, stringIdentifier, meshName);

            MeshHandler.AddStaticMesh(id);

            return id;
        }
    }
}