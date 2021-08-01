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
	/// <summary>
	/// The <see cref="IRegistry"/> class for all Archetypes
	/// </summary>
	class ArchetypeRegistry : IRegistry
	{
		/// <summary/>
		public delegate void RegisterDelegate();
		/// <summary/>
		public static event RegisterDelegate OnRegister = delegate {  };

		/// <inheritdoc/>
		public ushort RegistryID => RegistryIDs.Archetype;

		/// <inheritdoc/>
		public ICollection<ushort> RequiredRegistries => new ushort[] { RegistryIDs.Component };

		/// <summary>
		/// Register a Archetype
		/// </summary>
		/// <param name="archetype">The <see cref="ArchetypeContainer"/> for the Archetype</param>
		/// <param name="modId"><see cref="ushort"/> id of the mod registering the Archetype</param>
		/// <param name="stringIdentifier"><see cref="string"/> id of the Archetype></param>
		/// <returns>Generated <see cref="Identification"/> for the Archetype</returns>
		public static Identification RegisterArchetype(ArchetypeContainer archetype, ushort modId, string stringIdentifier )
		{
			Identification id = RegistryManager.RegisterObjectID( modId, RegistryIDs.Archetype, stringIdentifier );
			ArchetypeManager.AddArchetype( id, archetype );
			return id;
		}

		/// <inheritdoc/>
		public void Clear()
		{
			OnRegister = delegate {  };
			ArchetypeManager.Clear();
		}
		/// <inheritdoc/>
		public void PostRegister() { }
		/// <inheritdoc/>
		public void PreRegister() { }
		/// <inheritdoc/>
		public void Register()
		{
			Logger.WriteLog("Registering Archetypes", LogImportance.INFO, "Registry");
			OnRegister.Invoke();
		}
	}
}
