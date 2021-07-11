using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS
{
    public class EntityManager
    {
        private Dictionary<Identification, ArchetypeStorage> _archetypeStorages = new();
        private Dictionary<Identification, HashSet<uint>> _entityIDTracking = new();
        private Dictionary<Identification, uint> _lastFreeEntityID = new();
        private Dictionary<Entity, ushort> _entityOwner = new();

        private World _parent;

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

        public delegate void EntityCallback(World world, Entity entity);

        public static event EntityCallback PostEntityCreateEvent = delegate {  };
        public static event EntityCallback PreEntityDeleteEvent = delegate {  }; 

        public Entity CreateEntity(Identification archtypeId, ushort owner)
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

        public void DestroyEntity(Entity entity)
        {
            PreEntityDeleteEvent.Invoke(_parent, entity);
            _archetypeStorages[entity.ArchetypeID].RemoveEntity(entity);
            FreeEntityID(entity);
        }

        #region componentAccess

        public void SetComponent<Component>(Entity entity, Component component) where Component : unmanaged, IComponent
        {
            _archetypeStorages[entity.ArchetypeID].SetComponent(entity, component);
        }

        public unsafe void SetComponent<Component>(Entity entity, Component* component)
            where Component : unmanaged, IComponent
        {
            _archetypeStorages[entity.ArchetypeID].SetComponent(entity, component);
        }

        public Component GetComponent<Component>(Entity entity) where Component : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeID].GetComponent<Component>(entity);
        }

        public Component GetComponent<Component>(Entity entity, Identification componentID)
            where Component : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeID].GetComponent<Component>(entity, componentID);
        }

        public ref Component GetRefComponent<Component>(Entity entity) where Component : unmanaged, IComponent
        {
            return ref _archetypeStorages[entity.ArchetypeID].GetRefComponent<Component>(entity);
        }

        public ref Component GetRefComponent<Component>(Entity entity, Identification componentID)
            where Component : unmanaged, IComponent
        {
            return ref _archetypeStorages[entity.ArchetypeID].GetRefComponent<Component>(entity, componentID);
        }

        public unsafe Component* GetComponentPtr<Component>(Entity entity) where Component : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeID].GetComponentPtr<Component>(entity);
        }

        public unsafe Component* GetComponentPtr<Component>(Entity entity, Identification componentID)
            where Component : unmanaged, IComponent
        {
            return _archetypeStorages[entity.ArchetypeID].GetComponentPtr<Component>(entity, componentID);
        }

        #endregion
    }
}