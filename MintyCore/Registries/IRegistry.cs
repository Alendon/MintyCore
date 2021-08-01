using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Registries
{
	/// <summary>
	/// Interface for all registries
	/// </summary>
	public interface IRegistry
	{
		/// <summary>
		/// Method which get executed before the main registry
		/// </summary>
		void PreRegister();

		/// <summary>
		/// Main registry method
		/// </summary>
		void Register();

		/// <summary>
		/// Method which get executed after the main registry
		/// </summary>
		void PostRegister();

		/// <summary>
		/// Clear the registry. (Reset all registry events and dispose all created ressources)
		/// </summary>
		void Clear();

		/// <summary>
		/// The id of the registry
		/// </summary>
		ushort RegistryID { get; }

		/// <summary>
		/// Collection of registries which need to be processed before this
		/// </summary>
		ICollection<ushort> RequiredRegistries { get; }
	}
}
