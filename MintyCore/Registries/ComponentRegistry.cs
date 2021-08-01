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
	/// The <see cref="IRegistry"/> class for all <see cref="IComponent"/>
	/// </summary>
	public class ComponentRegistry : IRegistry
	{
		/// <summary/>
		public delegate void RegisterDelegate();

		/// <summary/>
		public static event RegisterDelegate OnRegister = delegate {  };

		/// <inheritdoc/>
		public ushort RegistryID => RegistryIDs.Component;

		/// <inheritdoc/>
		public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();

		/// <summary>
		/// Register the <typeparamref name="TComponent"/>
		/// </summary>
		/// <typeparam name="TComponent">Type of the Component to register. Must be <see href="unmanaged"/> and <see cref="IComponent"/></typeparam>
		/// <param name="modID"><see cref="ushort"/> id of the mod registering the <typeparamref name="TComponent"/></param>
		/// <param name="stringIdentifier"><see cref="string"/> id of the <typeparamref name="TComponent"/></param>
		/// <returns>Generated <see cref="Identification"/> for <typeparamref name="TComponent"/></returns>
		public static Identification RegisterComponent<TComponent>(ushort modID, string stringIdentifier ) where TComponent : unmanaged, IComponent
		{
			Identification componentID = RegistryManager.RegisterObjectID( modID, RegistryIDs.Component, stringIdentifier );
			ComponentManager.AddComponent<TComponent>( componentID );
			return componentID;
		}

		/// <inheritdoc/>
		public void Clear()
		{
			OnRegister = delegate {  };
			ComponentManager.Clear();
		}

		/// <inheritdoc/>
		public void PreRegister() { }

		/// <inheritdoc/>
		public void Register()
		{
			Logger.WriteLog("Registering Components", LogImportance.INFO, "Registry");
			OnRegister.Invoke();
		}

		/// <inheritdoc/>
		public void PostRegister() { }
	}
}
