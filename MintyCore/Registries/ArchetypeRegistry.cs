using System.Collections.Generic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Registries
{
	/// <summary>
	///     The <see cref="IRegistry" /> class for all Archetypes
	/// </summary>
	public class ArchetypeRegistry : IRegistry
    {
	    /// <summary />
	    public delegate void RegisterDelegate();

	    /// <inheritdoc />
	    public ushort RegistryId => RegistryIDs.Archetype;

	    /// <inheritdoc />
	    public IEnumerable<ushort> RequiredRegistries => new[] { RegistryIDs.Component };

	    /// <inheritdoc />
	    public void Clear()
        {
	        OnPostRegister = delegate { };
	        OnRegister = delegate { };
            ArchetypeManager.Clear();
        }

	    /// <inheritdoc />
	    public void PostRegister()
        {
	        OnPostRegister();
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
	    
	    /// <summary />
	    public static event RegisterDelegate OnPostRegister = delegate { };

	    /// <summary>
	    ///     Register a Archetype
	    /// </summary>
	    /// <param name="archetype">The <see cref="ArchetypeContainer" /> for the Archetype</param>
	    /// <param name="modId"><see cref="ushort" /> id of the mod registering the Archetype</param>
	    /// <param name="stringIdentifier"><see cref="string" /> id of the Archetype></param>
	    /// <returns>Generated <see cref="Identification" /> for the Archetype</returns>
	    public static Identification RegisterArchetype(ArchetypeContainer archetype, ushort modId,
            string stringIdentifier, IEntitySetup? setup = null)
        {
            var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Archetype, stringIdentifier);
            ArchetypeManager.AddArchetype(id, archetype);
            if(setup is not null) EntityManager.EntitySetups.Add(id, setup);
            return id;
        }

	    /// <summary>
	    /// Extend a <see cref="ArchetypeContainer"/>
	    /// Call this at PostRegister
	    /// </summary>
	    public static void ExtendArchetype(Identification archetypeId, ArchetypeContainer archetype)
	    {
		    ExtendArchetype(archetypeId, archetype.ArchetypeComponents);
	    }

	    /// <summary>
	    /// Extend a <see cref="ArchetypeContainer"/> with the given component <see cref="Identification"/>
	    /// Call this at PostRegister
	    /// </summary>
	    public static void ExtendArchetype(Identification archetypeId, IEnumerable<Identification> componentIDs)
	    {
		    ArchetypeManager.ExtendArchetype(archetypeId, componentIDs);
	    }
    }
}