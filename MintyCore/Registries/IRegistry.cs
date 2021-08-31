using System.Collections.Generic;

namespace MintyCore.Registries
{
	/// <summary>
	///     Interface for all registries
	/// </summary>
	public interface IRegistry
    {
	    /// <summary>
	    ///     The id of the registry
	    /// </summary>
	    ushort RegistryId { get; }

	    /// <summary>
	    ///     Collection of registries which need to be processed before this
	    /// </summary>
	    ICollection<ushort> RequiredRegistries { get; }

	    /// <summary>
	    ///     Method which get executed before the main registry
	    /// </summary>
	    void PreRegister();

	    /// <summary>
	    ///     Main registry method
	    /// </summary>
	    void Register();

	    /// <summary>
	    ///     Method which get executed after the main registry
	    /// </summary>
	    void PostRegister();

	    /// <summary>
	    ///     Clear the registry. (Reset all registry events and dispose all created ressources)
	    /// </summary>
	    void Clear();
    }
}