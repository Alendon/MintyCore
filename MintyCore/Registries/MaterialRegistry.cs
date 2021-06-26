using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Registries
{
    public class MaterialRegistry : IRegistry
    {
        public delegate void RegisterDelegate();

        public static event RegisterDelegate OnRegister = delegate { };

        public void PreRegister()
        {

        }

        public static Identification RegisterMaterial(ushort modId, string stringIdentifier, Pipeline pipeline, params (ResourceSet resourceSet, uint slot)[] resourceSets )
        {
            Identification materialId = RegistryManager.RegisterObjectID(modId, RegistryIDs.Material, stringIdentifier);
            MaterialHandler.AddMaterial(materialId, pipeline, resourceSets);
            return materialId;
        }

        public void Register()
        {
            OnRegister.Invoke();
        }
        
        public void PostRegister()
        {
        }

        public void Clear()
        {
            OnRegister = delegate {  };
            MaterialHandler.Clear();
        }


        public ushort RegistryID => RegistryIDs.Material;
        public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();
    }
}