using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.ECS
{
	public static class ArchetypeManager
	{
		private static Dictionary<Identification, ArchetypeContainer> _archetypes = new Dictionary<Identification, ArchetypeContainer>( _archetypes );

		internal static void AddArchetype( Identification archetypeID, ArchetypeContainer archetype )
		{
			_archetypes.TryAdd( archetypeID, archetype );
		}
		
		public static ArchetypeContainer GetArchetype(Identification archetypeID )
		{
			return _archetypes[archetypeID];
		}

		public static IReadOnlyDictionary<Identification, ArchetypeContainer> GetArchetypes()
		{
			return _archetypes;
		}

	}
}
