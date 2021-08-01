using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS
{
	/// <summary>
	/// Class to manage archetype specific stuff at init and runtime
	/// </summary>
	public static class ArchetypeManager
	{
		private static readonly Dictionary<Identification, ArchetypeContainer> _archetypes = new Dictionary<Identification, ArchetypeContainer>();

		internal static void AddArchetype( Identification archetypeID, ArchetypeContainer archetype )
		{
			_archetypes.TryAdd( archetypeID, archetype );
		}
		
		/// <summary>
		/// Get the ArchetypeContainer for a given archetype id
		/// </summary>
		/// <param name="archetypeID">id of the archetype</param>
		/// <returns>Container with the component ids of an archetype</returns>
		public static ArchetypeContainer GetArchetype(Identification archetypeID )
		{
			return _archetypes[archetypeID];
		}

		/// <summary>
		/// Get all registered archetype ids with their specific ArchetypeContainers
		/// </summary>
		/// <returns>ReadOnly Dictionary with archetype ids and ArchetypeContainers</returns>
		public static IReadOnlyDictionary<Identification, ArchetypeContainer> GetArchetypes()
		{
			return _archetypes;
		}

		internal static void Clear()
		{
			_archetypes.Clear();
		}

	}
}
