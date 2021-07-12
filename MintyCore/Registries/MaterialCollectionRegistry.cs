using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries
{
    public class MaterialCollectionRegistry : IRegistry
    {
        public delegate void RegisterDelegate();

        public static event RegisterDelegate OnRegister = delegate { };
        public void PreRegister()
        {
            
        }

        public void Register()
        {
            Logger.WriteLog("Registering MaterialCollections", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

        public void PostRegister()
        {
            
        }

        public static Identification RegisterMaterialCollection(ushort modID, string stringIdentifier,
            params Identification[] materialIDs)
        {
            Identification id =
                RegistryManager.RegisterObjectID(modID, RegistryIDs.MaterialCollection, stringIdentifier);
            MaterialHandler.AddMaterialCollection(id, materialIDs);
            return id;
        }

        public void Clear()
        {
            OnRegister = delegate {  };
            MaterialHandler.ClearCollections();
        }

        public ushort RegistryID => RegistryIDs.MaterialCollection;
        public ICollection<ushort> RequiredRegistries => new ushort[] { RegistryIDs.Material };
    }
}