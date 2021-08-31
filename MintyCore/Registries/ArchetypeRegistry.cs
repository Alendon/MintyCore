using System.Collections.Generic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Registries
{
	/// <summary>
	///     The <see cref="IRegistry" /> class for all Archetypes
	/// </summary>
	internal class ArchetypeRegistry : IRegistry
    {
	    /// <summary />
	    public delegate void RegisterDelegate();

	    /// <inheritdoc />
	    public ushort RegistryId => RegistryIDs.Archetype;

	    /// <inheritdoc />
	    public ICollection<ushort> RequiredRegistries => new[] { RegistryIDs.Component };

	    /// <inheritdoc />
	    public void Clear()
        {
            OnRegister = delegate { };
            ArchetypeManager.Clear();
        }

	    /// <inheritdoc />
	    public void PostRegister()
        {
        }

	    /// <inheritdoc />
	    public void PreRegister()
        {
        }

	    /// <inheritdoc />
	    public void Register()
        {
            Logger.WriteLog("Registering Archetypes", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

	    /// <summary />
	    public static event RegisterDelegate OnRegister = delegate { };

	    /// <summary>
	    ///     Register a Archetype
	    /// </summary>
	    /// <param name="archetype">The <see cref="ArchetypeContainer" /> for the Archetype</param>
	    /// <param name="modId"><see cref="ushort" /> id of the mod registering the Archetype</param>
	    /// <param name="stringIdentifier"><see cref="string" /> id of the Archetype></param>
	    /// <returns>Generated <see cref="Identification" /> for the Archetype</returns>
	    public static Identification RegisterArchetype(ArchetypeContainer archetype, ushort modId,
            string stringIdentifier)
        {
            var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Archetype, stringIdentifier);
            ArchetypeManager.AddArchetype(id, archetype);
            return id;
        }
    }
}