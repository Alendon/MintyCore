using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS
{
	public struct Entity : IEqualityComparer<Entity>, IEquatable<Entity>
	{
		public Entity(Identification archetypeID, uint id)
		{
			ArchetypeID = archetypeID;
			ID = id;
		}

		public Identification ArchetypeID { get; private set; }
		public uint ID { get; private set; }

		public override bool Equals(object? obj)
		{
			return obj is not null ? Equals((Entity)obj) : false;
		}

		public bool Equals(Entity x, Entity y)
		{
			return x.Equals(y);
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		public bool Equals(Entity other)
		{
			return ID == other.ID && ArchetypeID.Equals(other.ArchetypeID);
		}

		public override int GetHashCode() => HashCode.Combine(ArchetypeID, ID);

		public int GetHashCode([DisallowNull] Entity obj)
		{
			throw new NotImplementedException();
		}

		public static bool operator ==(Entity left, Entity right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Entity left, Entity right)
		{
			return !(left == right);
		}
	}
}
