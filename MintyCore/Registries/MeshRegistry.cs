using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries
{
    /// <summary>
	/// The <see cref="IRegistry"/> class for all <see cref="Mesh"/>
	/// </summary>
    public class MeshRegistry : IRegistry
    {
        /// <summary/>
        public delegate void RegisterDelegate();

        /// <summary/>
        public static event RegisterDelegate OnRegister = delegate { };

        /// <inheritdoc/>
        public void PreRegister()
        {
            
        }

        /// <inheritdoc/>
        public void Register()
        {
            Logger.WriteLog("Registering Meshes", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

        /// <inheritdoc/>
        public void PostRegister()
        {
            
        }

        /// <summary>
        /// Register a mesh
        /// </summary>
        /// <param name="modId"><see cref="ushort"/> id of the mod registering the <see cref="Mesh"/></param>
		/// <param name="stringIdentifier"><see cref="string"/> id of the <see cref="Mesh"/></param>
        /// <param name="meshName">The resource name of the <see cref="Mesh"/></param>
		/// <returns>Generated <see cref="Identification"/> for <see cref="Mesh"/></returns>
        public static Identification RegisterMesh(ushort modId, string stringIdentifier, string meshName)
		{
            Identification id = RegistryManager.RegisterObjectID(modId, RegistryIDs.Mesh, stringIdentifier, meshName);

            MeshHandler.AddStaticMesh(id);

            return id;
		}

        /// <inheritdoc/>
        public void Clear()
        {
           OnRegister = delegate {  };
           MeshHandler.Clear();
        }

        /// <inheritdoc/>
        public ushort RegistryID => RegistryIDs.Mesh;

        /// <inheritdoc/>
        public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();
    }
}