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
	/// The <see cref="IRegistry"/> class for all <see cref="ASystem"/>
	/// </summary>
	public class SystemRegistry : IRegistry
	{
		/// <summary/>
		public delegate void RegisterDelegate();
		/// <summary/>
		public static event RegisterDelegate OnRegister = delegate {  };

		/// <inheritdoc/>
		public ushort RegistryID => RegistryIDs.System;

		/// <inheritdoc/>
		public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();

		/// <summary>
		/// Register a <see cref="ASystem"/>
		/// </summary>
		/// <param name="modId"><see cref="ushort"/> id of the mod registering the <see cref="ASystem"/></param>
		/// <param name="stringIdentifier"><see cref="string"/> id of the <see cref="ASystem"/></param>
		/// <returns>Generated <see cref="Identification"/> for <see cref="ASystem"/></returns>
		public static Identification RegisterSystem<TSystem>(ushort modId, string stringIdentifier) where TSystem : ASystem, new()
		{
			Identification id = RegistryManager.RegisterObjectID( modId, RegistryIDs.System, stringIdentifier );
			SystemManager.RegisterSystem<TSystem>( id );
			return id;
		}

		/// <inheritdoc/>
		public void Clear()
		{
			OnRegister = delegate {  };
			SystemManager.Clear();
		}

		/// <inheritdoc/>
		public void PostRegister()
		{
			Logger.WriteLog("Post-Registering Systems", LogImportance.INFO, "Registry");
			SystemManager.SortSystems();
		}
		/// <inheritdoc/>
		public void PreRegister()
		{

		}
		/// <inheritdoc/>
		public void Register()
		{
			Logger.WriteLog("Registering Systems", LogImportance.INFO, "Registry");
			OnRegister.Invoke();
		}
	}
}
