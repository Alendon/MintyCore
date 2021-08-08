using MintyCore.Utils;

namespace MintyCore.Identifications
{
	/// <summary>
	/// <see langword="static"/> partial class which contains all archetype ids
	/// </summary>
	public static partial class ArchetypeIDs
	{
		/// <summary>
		/// <see cref="Identification"/> of the mesh archetype
		/// </summary>
		public static Identification Mesh;

		/// <summary>
		/// <see cref="Identification"/> of the player archetype
		/// </summary>
		public static Identification Player;

		public static Identification RigidBody { get; internal set; }
	}
}
