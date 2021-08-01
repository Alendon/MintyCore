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
	/// <summary>
	/// struct to identify a specific entity
	/// </summary>
	public struct Entity : IEqualityComparer<Entity>, IEquatable<Entity>
	{
		internal Entity(Identification archetypeID, uint id)
		{
			ArchetypeID = archetypeID;
			ID = id;
		}

		/// <summary>
		/// The archetype of the <see cref="Entity"/>
		/// </summary>
		public Identification ArchetypeID { get; private set; }

		/// <summary>
		/// The ID of the <see cref="Entity"/>
		/// </summary>
		public uint ID { get; private set; }

		/// <inheritdoc/>
		public override bool Equals(object? obj)
		{
			return obj is not null ? Equals((Entity)obj) : false;
		}

		/// <inheritdoc/>
		public bool Equals(Entity x, Entity y)
		{
			return x.Equals(y);
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		public bool Equals(Entity other)
		{
			return ID == other.ID && ArchetypeID.Equals(other.ArchetypeID);
		}

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(ArchetypeID, ID);

		/// <inheritdoc/>
		public int GetHashCode([DisallowNull] Entity obj)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public static bool operator ==(Entity left, Entity right)
		{
			return left.Equals(right);
		}

		/// <inheritdoc/>
		public static bool operator !=(Entity left, Entity right)
		{
			return !(left == right);
		}
	}
}
