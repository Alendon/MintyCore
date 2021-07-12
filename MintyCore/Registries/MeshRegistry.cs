using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries
{
    public class MeshRegistry : IRegistry
    {
        public delegate void RegisterDelegate();

        public static event RegisterDelegate OnRegister = delegate { };

        public void PreRegister()
        {
            
        }

        public void Register()
        {
            Logger.WriteLog("Registering Meshes", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

        public void PostRegister()
        {
            
        }

        public static Identification RegisterMesh(ushort modID, string stringIdentifier, string meshName)
		{
            Identification id = RegistryManager.RegisterObjectID(modID, RegistryIDs.Mesh, stringIdentifier, meshName);

            MeshHandler.AddStaticMesh(id);

            return id;
		}

        public void Clear()
        {
           OnRegister = delegate {  };
           MeshHandler.Clear();
        }

        public ushort RegistryID => RegistryIDs.Mesh;
        public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();
    }
}