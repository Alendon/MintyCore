using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.ECS
{
	public struct Entity
	{
		public Identification ArchetypeID { get; private set; }
		public ushort Owner { get; private set; }
		public ushort ID { get; private set; }

		public Entity( Identification archetypeID, ushort owner, ushort iD )
		{
			ArchetypeID = archetypeID;
			Owner = owner;
			ID = iD;
		}

		public static bool operator==(Entity entity1, Entity entity2 )
		{
			return entity1.Owner == entity2.Owner && entity1.ID == entity2.ID && entity1.ArchetypeID == entity2.ArchetypeID;
		}

		public static bool operator !=( Entity entity1, Entity entity2 )
		{
			return entity1.Owner != entity2.Owner || entity1.ID != entity2.ID || entity1.ArchetypeID != entity2.ArchetypeID;
		}
	}
}
