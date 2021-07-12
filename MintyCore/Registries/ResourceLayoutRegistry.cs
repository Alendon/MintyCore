using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace MintyCore.Registries
{
	class ResourceLayoutRegistry : IRegistry
	{
		public delegate void RegisterDelegate();
		public static event RegisterDelegate OnRegister = delegate { };

		public ushort RegistryID => RegistryIDs.ResourceLayout;

		public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();

		public static Identification RegisterResourceLayout(ushort modID, string stringIdentifier, ref ResourceLayoutDescription layoutDescription)
		{
			Identification id = RegistryManager.RegisterObjectID(modID, RegistryIDs.ResourceLayout, stringIdentifier);
			ResourceLayoutHandler.AddResourceLayout(id, ref layoutDescription);
			return id;
		}

		public void Clear()
		{
			ResourceLayoutHandler.Clear();
			OnRegister = delegate { };
		}

		public void PostRegister()
		{

		}

		public void PreRegister()
		{

		}

		public void Register()
		{
			Logger.WriteLog("Registering ResourceLayouts", LogImportance.INFO, "Registry");
			OnRegister.Invoke();
		}
	}
}
