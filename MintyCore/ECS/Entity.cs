using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS
{
	public struct Entity
	{
		public Entity( Identification archetypeID, uint id )
		{
			ArchetypeID = archetypeID;
			ID = id;
		}

		public Identification ArchetypeID { get; private set; }
		public uint ID { get; private set; }

		public override bool Equals( object obj ) => obj is Entity entity && ArchetypeID.Equals( entity.ArchetypeID ) && ID == entity.ID;
		public override int GetHashCode() => HashCode.Combine( ArchetypeID, ID );

		public static bool operator ==( Entity left, Entity right )
		{
			return left.Equals( right );
		}

		public static bool operator !=( Entity left, Entity right )
		{
			return !( left == right );
		}
	}
}
