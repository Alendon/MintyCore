using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Registries
{
	public class SystemRegistry : IRegistry
	{
		public delegate void RegisterDelegate();
		public static event RegisterDelegate OnRegister = delegate {  };

		public ushort RegistryID => RegistryIDs.System;

		public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();

		public static Identification RegisterSystem<T>(ushort modId, string stringIdentifier) where T : ASystem, new()
		{
			Identification id = RegistryManager.RegisterObjectID( modId, RegistryIDs.System, stringIdentifier );
			SystemManager.RegisterSystem<T>( id );
			return id;
		}

		public void Clear()
		{
			OnRegister = delegate {  };
			SystemManager.Clear();
		}
		public void PostRegister()
		{
			SystemManager.SortSystems();
		}
		public void PreRegister()
		{

		}
		public void Register()
		{
			OnRegister.Invoke();
		}
	}
}
