using System;
using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.ECS
{
    /// <summary>
    ///     Manage Entities per <see cref="World" />
    /// </summary>
    public class EntityManager : IDisposable
    {
        /// <summary>
        ///     EntityCallback delegate for entity specific events
        /// </summary>
        /// <param name="world"><see cref="World" /> the entity lives in</param>
        /// <param name="entity"></param>
        public delegate void EntityCallback(World world, Entity entity);

        private readonly Dictionary<Identification, ArchetypeStorage> _archetypeStorages = new();
        private readonly Dictionary<Identification, HashSet<uint>> _entityIdTracking = new();
        private readonly Dictionary<Entity, ushort> _entityOwner = new();
        private readonly Dictionary<Identification, uint> _lastFreeEntityId = new();

        private readonly World _parent;

        /// <summary>
        ///     Create a <see cref="EntityManager" /> for a world
        /// </summary>
        /// <param name="world"></param>
        public EntityManager(World world)
        {
            foreach (var item in ArchetypeManager.GetArchetypes())
            {
                _archetypeStorages.Add(item.Key, new ArchetypeStorage(item.Value, item.Key));
                _entityIdTracking.Add(item.Key, new HashSet<uint>());
                _lastFreeEntityId.Add(item.Key, Constants.InvalidId);
            }

            _parent = world;
        }

        /// <summary>
        ///     Get the entity count
        /// </summary>
        public object EntityCount
        {
            get
            {
                var count = 0;
                foreach (var storage in _archetypeStorages.Values) count += storage.EntityIndex.Count;

                return count;
            }
        }

        /// <summary>
        ///     Get the owner of an entity
        /// </summary>
        public ushort GetEntityOwner(Entity entity)
        {
            return _entityOwner.TryGetValue(entity, out var owner) ? owner : Constants.ServerId;
        }

        private Entity GetNextFreeEntityId(Identification archetype)
        {
            var archetypeTrack = _entityIdTracking[archetype];

            var id = _lastFreeEntityId[archetype];
            while (true)
            {
                id++;
                if (!archetypeTrack.Contains(id))
                {
                    archetypeTrack.Add(id);
                    _lastFreeEntityId[archetype] = id;
                    return new Entity(archetype, id);
                }

                if (id == uint.MaxValue) throw new Exception($"Maximum entity count for archetype {archetype} reached");
            }
        }

        private void FreeEntityId(Entity entity)
        {
            _entityIdTracking[entity.ArchetypeId].Remove(entity.Id);
            _lastFreeEntityId[entity.ArchetypeId] = entity.Id - 1;
        }

        internal ArchetypeStorage GetArchetypeStorage(Identification id)
        {
            return _archetypeStorages[id];
        }

        /// <summary>
        ///     Event which get fired directly after an entity is created
        /// </summary>
        public static event EntityCallback PostEntityCreateEvent = delegate { };

        /// <summary>
        ///     Event which get fired directly before an entity gets destroyed
        /// </summary>
        public static event EntityCallback PreEntityDeleteEvent = delegate { };

        /// <summary>
        ///     Create a new Entity
        /// </summary>
        /// <param name="archetypeId">Archetype of the entity</param>
        /// <param name="owner">Owner of the entity</param>
        /// <returns></returns>
        public Entity CreateEntity(Identification archetypeId, ushort owner = Constants.ServerId)
        {
            if (owner == Constants.InvalidId)
                throw new ArgumentException("Invalid entity owner");


            var entity = GetNextFreeEntityId(archetypeId);
            _archetypeStorages[archetypeId].AddEntity(entity);

            if (owner != Constants.ServerId)
                _entityOwner.Add(entity, owner);

            PostEntityCreateEvent.Invoke(_parent, entity);
            return entity;
        }

        /// <summary>
        ///     Destroy an <see cref="Entity" />
        /// </summary>
        /// <param name="entity"><see cref="Entity" /> to destroy</param>
        public void DestroyEntity(Entity entity)
        {
            PreEntityDeleteEvent.Invoke(_parent, entity);
            _archetypeStorages[entity.ArchetypeId].RemoveEntity(entity);
            FreeEntityId(entity);
        }

        #region componentAccess

        /// <summary>
        ///     Set the value of an <see cref="IComponent" /> of an <see cref="Entity" />
        /// </summary>
        public void SetComponent<TComponent>(Entity entity, TComponent component)
            where TComponent : unmanaged, IComponent
        {
            _archetypeStorages[entity.ArchetypeId].SetComponent(entity, component);
        }

        /// <summary>
        ///     Set the value of an <see cref="IComponent" /> of an <see cref="Entity" />
        /// </summary>
        public unsafe void SetComponent<TComponent>(Entity entity, TComponent* component)
            where TComponent : unmanaged, IComponent
        {
            _archetypeStorages[entity.ArchetypeId].SetComponent(entity, component);
        }

        /// <summary>
        ///     Get the value of an <see cref="IComponent" /> of an <see cref="Entity" />
        /// </summary>
        public TComponent GetComponent<TComponent>(Entity entity) where TComponent : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeId].GetComponent<TComponent>(entity);
        }

        /// <summary>
        ///     Get the value of an <see cref="IComponent" /> of an <see cref="Entity" />
        /// </summary>
        public TComponent GetComponent<TComponent>(Entity entity, Identification componentId)
            where TComponent : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeId].GetComponent<TComponent>(entity, componentId);
        }


        /// <summary>
        ///     Get the reference to an <see cref="IComponent" /> of an <see cref="Entity" />
        /// </summary>
        public ref TComponent GetRefComponent<TComponent>(Entity entity) where TComponent : unmanaged, IComponent
        {
            return ref _archetypeStorages[entity.ArchetypeId].GetRefComponent<TComponent>(entity);
        }

        /// <summary>
        ///     Get the reference to an <see cref="IComponent" /> of an <see cref="Entity" />
        /// </summary>
        public ref TComponent GetRefComponent<TComponent>(Entity entity, Identification componentId)
            where TComponent : unmanaged, IComponent
        {
            return ref _archetypeStorages[entity.ArchetypeId].GetRefComponent<TComponent>(entity, componentId);
        }

        /// <summary>
        ///     Get the pointer of an <see cref="IComponent" /> of an <see cref="Entity" />
        /// </summary>
        public unsafe TComponent* GetComponentPtr<TComponent>(Entity entity) where TComponent : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeId].GetComponentPtr<TComponent>(entity);
        }

        /// <summary>
        ///     Get the pointer of an <see cref="IComponent" /> of an <see cref="Entity" />
        /// </summary>
        public unsafe TComponent* GetComponentPtr<TComponent>(Entity entity, Identification componentId)
            where TComponent : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeId].GetComponentPtr<TComponent>(entity, componentId);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var archetypeWithIDs in _entityIdTracking)
            foreach (var ids in archetypeWithIDs.Value)
                PreEntityDeleteEvent(_parent, new Entity(archetypeWithIDs.Key, ids));

            foreach (var archetypeStorage in _archetypeStorages.Values) archetypeStorage.Dispose();
        }

        #endregion
    }
}