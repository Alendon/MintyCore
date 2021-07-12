using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries
{
	public class TextureRegistry : IRegistry
	{
		public delegate void RegisterDelegate();
		public static event RegisterDelegate OnRegister = delegate { };

		public static Identification RegisterTexture(ushort modID, string stringIdentifier, string textureName)
		{
			Identification id = RegistryManager.RegisterObjectID(modID, RegistryIDs.Texture, stringIdentifier, textureName);
			TextureHandler.AddTexture(id);
			return id;
		}

		public void PreRegister()
		{
		}

		public void Register()
		{
			Logger.WriteLog("Registering Textures", LogImportance.INFO, "Registry");
			OnRegister.Invoke();
		}

		public void PostRegister()
		{
		}

		public void Clear()
		{
			OnRegister = delegate { };
			TextureHandler.Clear();
		}

		public ushort RegistryID => RegistryIDs.Texture;
		public ICollection<ushort> RequiredRegistries => new ushort[]
			{
				RegistryIDs.ResourceLayout
			};
	}
}