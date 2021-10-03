using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.ECS
{
	/// <summary>
	///     Class to manage archetype specific stuff at init and runtime
	/// </summary>
	public static class ArchetypeManager
    {
        private static readonly Dictionary<Identification, ArchetypeContainer> _archetypes = new();

        internal static void AddArchetype(Identification archetypeId, ArchetypeContainer archetype)
        {
            _archetypes.TryAdd(archetypeId, archetype);
        }

        internal static void ExtendArchetype(Identification archetypeId, IEnumerable<Identification> componentIDs)
        {
            var container = _archetypes[archetypeId];
            foreach (var componentId in componentIDs)
            {
                container.ArchetypeComponents.Add(componentId);
            }
        }

        /// <summary>
        ///     Get the ArchetypeContainer for a given archetype id
        /// </summary>
        /// <param name="archetypeId">id of the archetype</param>
        /// <returns>Container with the component ids of an archetype</returns>
        public static ArchetypeContainer GetArchetype(Identification archetypeId)
        {
            return _archetypes[archetypeId];
        }

        /// <summary>
        ///     Get all registered archetype ids with their specific ArchetypeContainers
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