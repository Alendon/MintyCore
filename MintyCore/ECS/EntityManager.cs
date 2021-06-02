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
		private Dictionary<Identification, ArchetypeStorage> _archetypeStorages = new Dictionary<Identification, ArchetypeStorage>();
		private Dictionary<Identification, HashSet<uint>> _entityIDTracking = new Dictionary<Identification, HashSet<uint>>();
		private Dictionary<Entity, ushort> _entityOwner = new Dictionary<Entity, ushort>();

		public EntityManager()
		{
			foreach ( var item in ArchetypeManager.GetArchetypes() )
			{
				_archetypeStorages.Add( item.Key, new ArchetypeStorage( item.Value ) );
				_entityIDTracking.Add( item.Key, new HashSet<uint>() );
			}
		}

		private Entity GetFreeEntityID( Identification archetype )
		{
			var archtypeTrack = _entityIDTracking[archetype];

			uint id = Constants.InvalidID;
			while ( true )
			{
				id++;
				if ( !archtypeTrack.Contains( id ) )
				{
					archtypeTrack.Add( id );
					return new Entity( archetype, id) ;
				}
				if(id == uint.MaxValue )
				{
					throw new Exception( $"Maximum entity count for archetype {archetype} reached" );
				}
			}
		}
		private void FreeEntityID( Entity entity )
		{
			_entityIDTracking[entity.ArchetypeID].Remove( entity.ID );
		}

		internal ArchetypeStorage GetArchetypeStorage( Identification id )
		{
			return _archetypeStorages[id];
		}

		public delegate void EntityCallback( Entity entity );
		public event EntityCallback PostEntityCreateEvent = default;
		public event EntityCallback PreEntityDeleteEvent = default;

		public Entity CreateEntity( Identification archtypeID, ushort owner )
		{
			if ( owner == Constants.InvalidID )
				throw new ArgumentException( "Invalid entity owner" );
			

			Entity entity = GetFreeEntityID( archtypeID );
			_archetypeStorages[archtypeID].AddEntity( entity );

			if ( owner != Constants.ServerID )
				_entityOwner.Add( entity, owner );

			PostEntityCreateEvent?.Invoke(entity);
			return entity;
		}

		public void DestroyEntity(Entity entity )
		{
			PreEntityDeleteEvent?.Invoke( entity );
			_archetypeStorages[entity.ArchetypeID].RemoveEntity( entity );
			FreeEntityID( entity );
		}

		#region componentAccess
		public void SetComponent<Component>(Entity entity, Component component) where Component : unmanaged, IComponent
		{
			_archetypeStorages[entity.ArchetypeID].SetComponent( entity, component );
		}
		public unsafe void SetComponent<Component>( Entity entity, Component* component ) where Component : unmanaged, IComponent
		{
			_archetypeStorages[entity.ArchetypeID].SetComponent( entity, component );
		}

		public Component GetComponent<Component>(Entity entity) where Component : unmanaged, IComponent
		{
			return _archetypeStorages[entity.ArchetypeID].GetComponent<Component>( entity );
		}
		public Component GetComponent<Component>( Entity entity, Identification componentID ) where Component : unmanaged, IComponent
		{
			return _archetypeStorages[entity.ArchetypeID].GetComponent<Component>( entity, componentID );
		}

		public ref Component GetRefComponent<Component>( Entity entity ) where Component : unmanaged, IComponent
		{
			return ref _archetypeStorages[entity.ArchetypeID].GetRefComponent<Component>( entity );
		}
		public ref Component GetRefComponent<Component>( Entity entity, Identification componentID ) where Component : unmanaged, IComponent
		{
			return ref _archetypeStorages[entity.ArchetypeID].GetRefComponent<Component>( entity, componentID );
		}

		public unsafe Component* GetComponentPtr<Component>( Entity entity ) where Component : unmanaged, IComponent
		{
			return _archetypeStorages[entity.ArchetypeID].GetComponentPtr<Component>( entity );
		}
		public unsafe Component* GetComponentPtr<Component>( Entity entity, Identification componentID ) where Component : unmanaged, IComponent
		{
			return _archetypeStorages[entity.ArchetypeID].GetComponentPtr<Component>( entity, componentID );
		}
		#endregion
	}
}
