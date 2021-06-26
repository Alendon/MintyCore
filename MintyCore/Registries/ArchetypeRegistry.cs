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
	class ArchetypeRegistry : IRegistry
	{
		public delegate void RegisterDelegate();
		public static event RegisterDelegate OnRegister = delegate {  };

		public ushort RegistryID => RegistryIDs.Archetype;

		public ICollection<ushort> RequiredRegistries => new ushort[] { RegistryIDs.Component };

		public static Identification RegisterArchetype(ArchetypeContainer archetype, ushort modID, string stringIdentifier )
		{
			Identification id = RegistryManager.RegisterObjectID( modID, RegistryIDs.Archetype, stringIdentifier );
			ArchetypeManager.AddArchetype( id, archetype );
			return id;
		}

		public void Clear()
		{
			OnRegister = delegate {  };
			ArchetypeManager.Clear();
		}
		public void PostRegister() { }
		public void PreRegister() { }
		public void Register()
		{
			OnRegister.Invoke();
		}
	}
}
