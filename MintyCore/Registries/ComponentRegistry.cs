using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.Registries
{
	public class ComponentRegistry : IRegistry
	{
		public delegate void RegisterDelegate();
		public static event RegisterDelegate OnRegister;

		public ushort RegistryID => RegistryIDs.Component;

		public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();

		public static Identification RegisterComponent<T>(ushort modID, string stringIdentifier ) where T : unmanaged, IComponent
		{
			Identification componentID = RegistryManager.RegisterObjectID( modID, RegistryIDs.Component, stringIdentifier );
			ComponentManager.AddComponent<T>( componentID );
			return componentID;
		}

		public void Clear()
		{
			OnRegister = default;
			ComponentManager.Clear();
		}

		public void PreRegister() { }
		public void Register()
		{
			OnRegister.Invoke();
		}
		public void PostRegister() { }
	}
}
