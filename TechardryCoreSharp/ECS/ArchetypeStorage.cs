using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Components;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.ECS
{
	internal unsafe class ArchetypeStorage
	{
		private IntPtr _data;
		private readonly int _archetypeSize = 0;
		private readonly Dictionary<Identification, int> _componentOffsets = new Dictionary<Identification, int>();

		private const int _defaultStorageSize = 16;
		private int _entityCount = 0;
		private int _storageSize = _defaultStorageSize;


		//Uint as entity id, as this the combination of the entity owner and entity id
		//Key: Entity, Value: Index
		internal Dictionary<Entity, int> _entityIndex = new Dictionary<Entity, int>( _defaultStorageSize );

		//Index (of Array): Index (in Memory), Value: Entity
		private Entity[] _indexEntity = new Entity[_defaultStorageSize];

		const int entityIndexSearchPivot = -1;


		internal ArchetypeStorage( ArchetypeContainer archetype )
		{
			int lastComponentOffset = 0;

			foreach ( var componentID in archetype.ArchetypeComponents )
			{
				int componentSize = ComponentManager.GetComponentSize( componentID );
				_archetypeSize += componentSize;
				_componentOffsets.Add( componentID, lastComponentOffset );
				lastComponentOffset += componentSize;
			}

			_data = AllocationHandler.Malloc( _archetypeSize * _storageSize );
			Array.Resize( ref _indexEntity, _storageSize );
		}

		private ArchetypeStorage() { }

		internal Component GetComponent<Component>( Entity entity ) where Component : unmanaged, IComponent
		{
			Component component = default;
			return GetComponent<Component>( entity, component.Identification );
		}

		internal Component GetComponent<Component>( Entity entity, Identification componentID ) where Component : unmanaged, IComponent
		{
			return *( Component* )( _data + ( _entityIndex[entity] * _archetypeSize ) + _componentOffsets[componentID] );
		}

		internal ref Component GetRefComponent<Component>( Entity entity ) where Component : unmanaged, IComponent
		{
			Component component = default;
			return ref GetRefComponent<Component>( entity, component.Identification );
		}

		internal ref Component GetRefComponent<Component>( Entity entity, Identification componentID ) where Component : unmanaged, IComponent
		{
			return ref *( Component* )( _data + ( _entityIndex[entity] * _archetypeSize ) + _componentOffsets[componentID] );
		}

		internal void SetComponent<Component>( Entity entity, Component component ) where Component : unmanaged, IComponent
		{
			*( Component* )( _data + ( _entityIndex[entity] * _archetypeSize ) + _componentOffsets[component.Identification] ) = component;
		}

		internal void SetComponent<Component>( Entity entity, Component* component ) where Component : unmanaged, IComponent
		{
			*( Component* )( _data + ( _entityIndex[entity] * _archetypeSize ) + _componentOffsets[component->Identification] ) = *component;
		}

		internal Component* GetComponentPtr<Component>(Entity entity, Identification componentID) where Component : unmanaged, IComponent
		{
			return ( Component* )( _data + ( _entityIndex[entity] * _archetypeSize ) + _componentOffsets[componentID] );
		}

		internal Component* GetComponentPtr<Component>( Entity entity ) where Component : unmanaged, IComponent
		{
			Component component = default;
			return ( Component* )( _data + ( _entityIndex[entity] * _archetypeSize ) + _componentOffsets[component.Identification] );
		}


		internal void AddEntity( Entity entity )
		{
			if ( _entityIndex.ContainsKey( entity ) )
			{
				throw new Exception( $"Entity to add ({entity}) is already present" );
			}

			if ( _entityCount >= _storageSize )
			{
				Resize( _entityCount * 2 );
			}

			int freeIndex = entityIndexSearchPivot;
			if ( !FindNextFreeIndex( ref freeIndex ) )
			{
				freeIndex = -1;
				if ( !FindNextFreeIndex( ref freeIndex ) )
				{
					throw new Exception( "Unknown Error happened" );
				}
			}

			_entityIndex.Add( entity, freeIndex );
			_indexEntity[freeIndex] = entity;
			_entityCount++;

			IntPtr entityData = _data + ( freeIndex * _archetypeSize );
			foreach ( var entry in _componentOffsets )
			{
				var componentID = entry.Key;
				var componentOffset = entry.Value;
				ComponentManager.PopulateComponentDefaultValues( componentID, entityData + componentOffset );

			}

		}

		internal void RemoveEntity( Entity entity )
		{
			if ( !_entityIndex.ContainsKey( entity ) )
			{
				throw new ArgumentException( $" Entity {entity} not present" );
			}

			_entityCount--;
			int index = _entityIndex[entity];
			_entityIndex.Remove( entity );
			_indexEntity[index] = default;

			if ( _entityCount * 4 <= _storageSize && _storageSize > _defaultStorageSize )
			{
				Resize( _storageSize / 2 );
			}
		}


		/// <summary>
		/// Finde next free entityIndex
		/// </summary>
		/// <param name="previousIndex">The last known free index. If unknown use -1</param>
		/// <returns>Returns true if an free index was found</returns>
		private bool FindNextFreeIndex( ref int previousIndex )
		{
			do
			{
				previousIndex++;
				if ( previousIndex >= _storageSize )
				{
					return false;
				}
			} while ( _indexEntity[previousIndex] != default );

			return true;
		}

		private bool FindPreviousTakenIndex( ref int previousIndex )
		{
			do
			{
				previousIndex--;
				if ( previousIndex < 0 )
				{
					return false;
				}
			} while ( _indexEntity[previousIndex] == ( Entity )default );
			return true;
		}

		private void CopyEntityFromTo( int oldEntityIndex, int newEntityIndex )
		{
			void* oldDataLocation = ( void* )( _data + ( oldEntityIndex * _archetypeSize ) );
			void* newDataLocation = ( void* )( _data + ( newEntityIndex * _archetypeSize ) );

			Buffer.MemoryCopy( oldDataLocation, newDataLocation, _archetypeSize, _archetypeSize );
		}

		private void CompactData()
		{
			int freeIndex = -1;
			int takenIndex = _storageSize;

			while ( true )
			{
				if ( !FindNextFreeIndex( ref freeIndex ) || !FindPreviousTakenIndex( ref takenIndex ) )
				{
					break;
				}
				if ( freeIndex >= takenIndex )
				{
					break;
				}

				Entity entity = _indexEntity[takenIndex];

				_entityIndex[entity] = freeIndex;
				_indexEntity[takenIndex] = default;
				_indexEntity[freeIndex] = entity;

				CopyEntityFromTo( takenIndex, freeIndex );
			}
		}

		private void Resize( int newSize )
		{
			if ( newSize == _storageSize ) return;

			if ( newSize < _storageSize )
			{
				if ( newSize < _entityCount )
				{
					throw new Exception( $"The new size ({newSize}) of the archetype storage is smaller then the current entity count ({_entityCount})" );
				}
				CompactData();
			}

			void* oldData = ( void* )_data;
			void* newData = ( void* )AllocationHandler.Malloc( newSize * _archetypeSize );

			var bytesToCopy = newSize > _storageSize ? _storageSize * _archetypeSize : newSize * _archetypeSize;

			Buffer.MemoryCopy( oldData, newData, bytesToCopy, bytesToCopy );

			AllocationHandler.Free( ( IntPtr )oldData );
			_data = ( IntPtr )newData;
			Array.Resize( ref _indexEntity, newSize );
			_storageSize = newSize;
		}

	}
}
