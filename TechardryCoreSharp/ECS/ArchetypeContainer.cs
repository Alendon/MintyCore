using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.ECS
{
	public class ArchetypeContainer
	{
		internal HashSet<Identification> ArchetypeComponents { private set; get; }

		public ArchetypeContainer( HashSet<Identification> archetypeComponents ) => ArchetypeComponents = archetypeComponents;
		public ArchetypeContainer(IEnumerable<Identification> archtypeComponents )
		{
			ArchetypeComponents = new HashSet<Identification>( archtypeComponents );
		}

		public void AddComponents(params Identification[] components )
		{
			foreach(var entry in components )
			{
				ArchetypeComponents.Add( entry );
			}
		}
	}
}
