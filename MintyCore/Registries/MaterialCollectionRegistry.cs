using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries
{
    /// <summary>
	/// The <see cref="IRegistry"/> class for all MaterialCollections
	/// </summary>
    public class MaterialCollectionRegistry : IRegistry
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
            Logger.WriteLog("Registering MaterialCollections", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

        /// <inheritdoc/>
        public void PostRegister()
        {
            
        }

        /// <summary>
        /// Register a MaterialCollection
        /// </summary>
        /// <param name="modID"><see cref="ushort"/> id of the mod registering the MaterialCollection</param>
		/// <param name="stringIdentifier"><see cref="string"/> id of the MaterialCollection</param>
        /// <param name="materialIDs">Collection of Material <see cref="Identification"/></param>
		/// <returns>Generated <see cref="Identification"/> for MaterialCollection</returns>
        public static Identification RegisterMaterialCollection(ushort modID, string stringIdentifier,
            params Identification[] materialIDs)
        {
            Identification id =
                RegistryManager.RegisterObjectID(modID, RegistryIDs.MaterialCollection, stringIdentifier);
            MaterialHandler.AddMaterialCollection(id, materialIDs);
            return id;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            OnRegister = delegate {  };
            MaterialHandler.ClearCollections();
        }

        /// <inheritdoc/>
        public ushort RegistryID => RegistryIDs.MaterialCollection;
        /// <inheritdoc/>
        public ICollection<ushort> RequiredRegistries => new ushort[] { RegistryIDs.Material };
    }
}