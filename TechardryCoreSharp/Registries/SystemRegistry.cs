using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.ECS;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.Registries
{
	public class SystemRegistry : IRegistry
	{
		public delegate void RegisterDelegate();
		public static event RegisterDelegate OnRegister;

		public ushort RegistryID => RegistryIDs.System;

		public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();

		public static Identification RegisterSystem<T>(ushort modID, string stringIdentifier) where T : ASystem, new()
		{
			Identification id = RegistryManager.RegisterObjectID( modID, RegistryIDs.System, stringIdentifier );
			SystemManager.RegisterSystem<T>( id );
			return id;
		}

		public void Clear()
		{
			OnRegister = default;
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
