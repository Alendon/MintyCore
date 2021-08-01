using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS
{
    /// <summary>
    /// Manage Entities per <see cref="World"/>
    /// </summary>
    public class EntityManager : IDisposable
    {
        private Dictionary<Identification, ArchetypeStorage> _archetypeStorages = new();
        private Dictionary<Identification, HashSet<uint>> _entityIDTracking = new();
        private Dictionary<Identification, uint> _lastFreeEntityID = new();
        private Dictionary<Entity, ushort> _entityOwner = new();

        private World _parent;

        /// <summary>
        /// Create a <see cref="EntityManager"/> for a world
        /// </summary>
        /// <param name="world"></param>
        public EntityManager(World world)
        {
            foreach (var item in ArchetypeManager.GetArchetypes())
            {
                _archetypeStorages.Add(item.Key, new ArchetypeStorage(item.Value));
                _entityIDTracking.Add(item.Key, new HashSet<uint>());
                _lastFreeEntityID.Add(item.Key, Constants.InvalidID);
            }

            _parent = world;
        }

        private Entity GetFreeEntityID(Identification archetype)
        {
            var archtypeTrack = _entityIDTracking[archetype];

            uint id = _lastFreeEntityID[archetype];
            while (true)
            {
                id++;
                if (!archtypeTrack.Contains(id))
                {
                    archtypeTrack.Add(id);
                    _lastFreeEntityID[archetype] = id;
                    return new Entity(archetype, id);
                }

                if (id == uint.MaxValue)
                {
                    throw new Exception($"Maximum entity count for archetype {archetype} reached");
                }
            }
        }

        private void FreeEntityID(Entity entity)
        {
            _entityIDTracking[entity.ArchetypeID].Remove(entity.ID);
            _lastFreeEntityID[entity.ArchetypeID] = entity.ID - 1;
        }

        internal ArchetypeStorage GetArchetypeStorage(Identification id)
        {
            return _archetypeStorages[id];
        }

        /// <summary>
        /// EntityCallback delegate for entity specific events
        /// </summary>
        /// <param name="world"><see cref="World"/> the entity lives in</param>
        /// <param name="entity"></param>
        public delegate void EntityCallback(World world, Entity entity);

        /// <summary>
        /// Event which get fired directly after an entity is created
        /// </summary>
        public static event EntityCallback PostEntityCreateEvent = delegate {  };

        /// <summary>
        /// Event which get fired directly before an entity gets destroyed
        /// </summary>
        public static event EntityCallback PreEntityDeleteEvent = delegate {  }; 

        /// <summary>
        /// Create a new Entity
        /// </summary>
        /// <param name="archtypeId">Archtype of the entity</param>
        /// <param name="owner">Owner of the entity</param>
        /// <returns></returns>
        public Entity CreateEntity(Identification archtypeId, ushort owner = Constants.ServerID)
        {
            if (owner == Constants.InvalidID)
                throw new ArgumentException("Invalid entity owner");


            Entity entity = GetFreeEntityID(archtypeId);
            _archetypeStorages[archtypeId].AddEntity(entity);

            if (owner != Constants.ServerID)
                _entityOwner.Add(entity, owner);

            PostEntityCreateEvent.Invoke(_parent, entity);
            return entity;
        }

        /// <summary>
        /// Destroy an <see cref="Entity"/>
        /// </summary>
        /// <param name="entity"><see cref="Entity"/> to destroy</param>
        public void DestroyEntity(Entity entity)
        {
            PreEntityDeleteEvent.Invoke(_parent, entity);
            _archetypeStorages[entity.ArchetypeID].RemoveEntity(entity);
            FreeEntityID(entity);
        }

        #region componentAccess

        /// <summary>
        /// Set the value of an <see cref="IComponent"/> of an <see cref="Entity"/>
        /// </summary>
        public void SetComponent<Component>(Entity entity, Component component) where Component : unmanaged, IComponent
        {
            _archetypeStorages[entity.ArchetypeID].SetComponent(entity, component);
        }

        /// <summary>
        /// Set the value of an <see cref="IComponent"/> of an <see cref="Entity"/>
        /// </summary>
        public unsafe void SetComponent<Component>(Entity entity, Component* component)
            where Component : unmanaged, IComponent
        {
            _archetypeStorages[entity.ArchetypeID].SetComponent(entity, component);
        }

        /// <summary>
        /// Get the value of an <see cref="IComponent"/> of an <see cref="Entity"/>
        /// </summary>
        public Component GetComponent<Component>(Entity entity) where Component : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeID].GetComponent<Component>(entity);
        }

        /// <summary>
        /// Get the value of an <see cref="IComponent"/> of an <see cref="Entity"/>
        /// </summary>
        public Component GetComponent<Component>(Entity entity, Identification componentID)
            where Component : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeID].GetComponent<Component>(entity, componentID);
        }


        /// <summary>
        /// Get the reference to an <see cref="IComponent"/> of an <see cref="Entity"/>
        /// </summary>
        public ref Component GetRefComponent<Component>(Entity entity) where Component : unmanaged, IComponent
        {
            return ref _archetypeStorages[entity.ArchetypeID].GetRefComponent<Component>(entity);
        }

        /// <summary>
        /// Get the reference to an <see cref="IComponent"/> of an <see cref="Entity"/>
        /// </summary>
        public ref Component GetRefComponent<Component>(Entity entity, Identification componentID)
            where Component : unmanaged, IComponent
        {
            return ref _archetypeStorages[entity.ArchetypeID].GetRefComponent<Component>(entity, componentID);
        }

        /// <summary>
        /// Get the pointer of an <see cref="IComponent"/> of an <see cref="Entity"/>
        /// </summary>
        public unsafe Component* GetComponentPtr<Component>(Entity entity) where Component : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeID].GetComponentPtr<Component>(entity);
        }

        /// <summary>
        /// Get the pointer of an <see cref="IComponent"/> of an <see cref="Entity"/>
        /// </summary>
        public unsafe Component* GetComponentPtr<Component>(Entity entity, Identification componentID)
            where Component : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeID].GetComponentPtr<Component>(entity, componentID);
        }

        /// <inheritdoc/>
		public void Dispose()
		{
			foreach (var archetypeStorage in _archetypeStorages.Values)
			{
                archetypeStorage.Dispose();
			}
		}

		#endregion
	}
}