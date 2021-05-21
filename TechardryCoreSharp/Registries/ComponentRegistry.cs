using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.ECS;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.Registries
{
	public class ComponentRegistry : IRegistry
	{
		public delegate void RegisterDelegate();
		public static event RegisterDelegate onRegister;

		public ushort RegistryID => RegistryIDs.Component;

		public ICollection<ushort> RequiredRegistries => new ushort[0];

		public static Identification RegisterComponent<T>(ushort ModID, string stringIdentifier ) where T : unmanaged, IComponent
		{
			Identification componentID = RegistryManager.RegisterObjectID( ModID, RegistryIDs.Component, stringIdentifier );
			ComponentManager.AddComponent<T>( componentID );
			return componentID;
		}

		public void Clear()
		{
			ComponentManager.Clear();
		}

		public void PreRegister() { }
		public void Register()
		{
			onRegister.Invoke();
		}
		public void PostRegister() { }
	}
}
