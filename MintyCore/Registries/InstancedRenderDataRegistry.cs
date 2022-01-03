
using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries
{
    public class InstancedRenderDataRegistry : IRegistry
    {
        public ushort RegistryId => RegistryIDs.IndexRenderData;
        public IEnumerable<ushort> RequiredRegistries => new[]{
            RegistryIDs.Mesh, RegistryIDs.Material
        };
        
        
        /// <summary />
        public delegate void RegisterDelegate();

        /// <summary />
        public static event RegisterDelegate OnRegister = delegate { };
        
        public void PreRegister()
        {
        }

        public void Register()
        {
            OnRegister();
        }

        public static Identification RegisterInstancedRenderData(ushort modId, string stringIdentifier,
            Identification meshId, params Identification[] materialIds)
        {
            Identification id = RegistryManager.RegisterObjectId(modId, RegistryIDs.IndexRenderData, stringIdentifier);
            InstancedRenderDataHandler.AddMeshMaterial(id, MeshHandler.GetStaticMesh(meshId), materialIds.Select(MaterialHandler.GetMaterial).ToArray());
            return id;
        }

        public void PostRegister()
        {
        }

        public void Clear()
        {
            InstancedRenderDataHandler.Clear();
            OnRegister = delegate {  };
        }
    }
}