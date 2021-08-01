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
	/// <summary>
	/// The <see cref="IRegistry"/> class for all <see cref="ResourceLayout"/>
	/// </summary>
	class ResourceLayoutRegistry : IRegistry
	{
		/// <summary/>
		public delegate void RegisterDelegate();
		/// <summary/>
		public static event RegisterDelegate OnRegister = delegate { };

		/// <inheritdoc/>
		public ushort RegistryID => RegistryIDs.ResourceLayout;

		/// <inheritdoc/>
		public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();

		/// <summary>
		/// Register a <see cref="ResourceLayout"/>
		/// </summary>
		/// <param name="modId"><see cref="ushort"/> id of the mod registering the <see cref="ResourceLayout"/></param>
		/// <param name="stringIdentifier"><see cref="string"/> id of the <see cref="ResourceLayout"/></param>
		/// <param name="layoutDescription">The <see cref="ResourceLayoutDescription"/> of the <see cref="ResourceLayout"/></param>
		/// <returns>Generated <see cref="Identification"/> for <see cref="ResourceLayout"/></returns>
		public static Identification RegisterResourceLayout(ushort modId, string stringIdentifier, ref ResourceLayoutDescription layoutDescription)
		{
			Identification id = RegistryManager.RegisterObjectID(modId, RegistryIDs.ResourceLayout, stringIdentifier);
			ResourceLayoutHandler.AddResourceLayout(id, ref layoutDescription);
			return id;
		}

		/// <inheritdoc/>
		public void Clear()
		{
			ResourceLayoutHandler.Clear();
			OnRegister = delegate { };
		}

		/// <inheritdoc/>
		public void PostRegister()
		{

		}

		/// <inheritdoc/>
		public void PreRegister()
		{

		}

		/// <inheritdoc/>
		public void Register()
		{
			Logger.WriteLog("Registering ResourceLayouts", LogImportance.INFO, "Registry");
			OnRegister.Invoke();
		}
	}
}
