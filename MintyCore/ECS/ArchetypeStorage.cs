using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Components;
using MintyCore.Utils;

namespace MintyCore.ECS
{

	[DebuggerTypeProxy(typeof(DebugView))]
	internal unsafe class ArchetypeStorage : IDisposable
	{
		public IntPtr _data { get; private set; }
		public readonly int _archetypeSize = 0;
		public readonly Dictionary<Identification, int> _componentOffsets = new();
		private ArchetypeContainer _archetype;
		public Identification ID { get; private set; }

		private const int _defaultStorageSize = 16;
		private int _entityCount = 0;
		private int _storageSize = _defaultStorageSize;


		//Key: Entity, Value: Index
		internal Dictionary<Entity, int> _entityIndex = new Dictionary<Entity, int>(_defaultStorageSize);

		//Index (of Array): Index (in Memory), Value: Entity
		internal Entity[] _indexEntity = new Entity[_defaultStorageSize];

		private int entityIndexSearchPivot = -1;


		internal ArchetypeStorage(ArchetypeContainer archetype, Identification archetypeID)
		{
			int lastComponentOffset = 0;
			_archetype = archetype;

			foreach (var componentID in archetype.ArchetypeComponents)
			{
				int componentSize = ComponentManager.GetComponentSize(componentID);
				_archetypeSize += componentSize;
				_componentOffsets.Add(componentID, lastComponentOffset);
				lastComponentOffset += componentSize;
			}

			_data = AllocationHandler.Malloc(_archetypeSize * _storageSize);
			Array.Resize(ref _indexEntity, _storageSize);
			ID = archetypeID;
		}


		internal Component GetComponent<Component>(Entity entity) where Component : unmanaged, IComponent
		{
			Component component = default;
			return GetComponent<Component>(entity, component.Identification);
		}

		internal Component GetComponent<Component>(Entity entity, Identification componentID) where Component : unmanaged, IComponent
		{
			return *GetComponentPtr<Component>(entity, componentID);
		}

		internal ref Component GetRefComponent<Component>(Entity entity) where Component : unmanaged, IComponent
		{
			Component component = default;
			return ref GetRefComponent<Component>(entity, component.Identification);
		}

		internal ref Component GetRefComponent<Component>(Entity entity, Identification componentID) where Component : unmanaged, IComponent
		{
			return ref *GetComponentPtr<Component>(entity, componentID);
		}

		internal void SetComponent<Component>(Entity entity, Component component) where Component : unmanaged, IComponent
		{
			*GetComponentPtr<Component>(entity, component.Identification) = component;
		}

		internal void SetComponent<Component>(Entity entity, Component* component) where Component : unmanaged, IComponent
		{
			*GetComponentPtr<Component>(entity, component->Identification) = *component;
		}

		internal IntPtr GetComponentPtr(Entity entity, Identification componentID)
		{
			return GetComponentPtr(_entityIndex[entity], componentID);
		}

		internal IntPtr GetComponentPtr(int entityIndex, Identification componentID)
		{
			return _data + (entityIndex * _archetypeSize) + _componentOffsets[componentID];
		}

		internal Component* GetComponentPtr<Component>(Entity entity, Identification componentID) where Component : unmanaged, IComponent
		{
			return (Component*)GetComponentPtr(entity, componentID);
		}

		internal Component* GetComponentPtr<Component>(Entity entity) where Component : unmanaged, IComponent
		{
			Component component = default;
			return GetComponentPtr<Component>(entity, component.Identification);
		}

		internal Component* GetComponentPtr<Component>(int entityIndex, Identification componentID) where Component : unmanaged, IComponent
		{
			return (Component*)GetComponentPtr(entityIndex, componentID);
		}


		internal void AddEntity(Entity entity)
		{
			if (_entityIndex.ContainsKey(entity))
			{
				throw new Exception($"Entity to add ({entity}) is already present");
			}

			if (_entityCount >= _storageSize)
			{
				Resize(_entityCount * 2);
			}

			int freeIndex = entityIndexSearchPivot;
			if (!FindNextFreeIndex(ref freeIndex))
			{
				freeIndex = -1;
				if (!FindNextFreeIndex(ref freeIndex))
				{
					throw new Exception("Unknown Error happened");
				}
			}

			_entityIndex.Add(entity, freeIndex);
			_indexEntity[freeIndex] = entity;
			_entityCount++;
			entityIndexSearchPivot = freeIndex;

			IntPtr entityData = _data + (freeIndex * _archetypeSize);
			foreach (var entry in _componentOffsets)
			{
				var componentID = entry.Key;
				var componentOffset = entry.Value;
				ComponentManager.PopulateComponentDefaultValues(componentID, entityData + componentOffset);

			}

		}

		internal void RemoveEntity(Entity entity)
		{
			if (!_entityIndex.ContainsKey(entity))
			{
				throw new ArgumentException($" Entity {entity} not present");
			}

			_entityCount--;
			int index = _entityIndex[entity];
			_entityIndex.Remove(entity);
			_indexEntity[index] = default;

			if (_entityCount * 4 <= _storageSize && _storageSize > _defaultStorageSize)
			{
				Resize(_storageSize / 2);
			}
		}


		/// <summary>
		/// Finde next free entityIndex
		/// </summary>
		/// <param name="previousIndex">The last known free index. If unknown use -1</param>
		/// <returns>Returns true if an free index was found</returns>
		private bool FindNextFreeIndex(ref int previousIndex)
		{
			do
			{
				previousIndex++;
				if (previousIndex >= _storageSize)
				{
					return false;
				}
			} while (_indexEntity[previousIndex] != default);

			return true;
		}

		private bool FindPreviousTakenIndex(ref int previousIndex)
		{
			do
			{
				previousIndex--;
				if (previousIndex < 0)
				{
					return false;
				}
			} while (_indexEntity[previousIndex] == (Entity)default);
			return true;
		}

		private void CopyEntityFromTo(int oldEntityIndex, int newEntityIndex)
		{
			void* oldDataLocation = (void*)(_data + (oldEntityIndex * _archetypeSize));
			void* newDataLocation = (void*)(_data + (newEntityIndex * _archetypeSize));

			Buffer.MemoryCopy(oldDataLocation, newDataLocation, _archetypeSize, _archetypeSize);
		}

		private void CompactData()
		{
			int freeIndex = -1;
			int takenIndex = _storageSize;

			while (true)
			{
				if (!FindNextFreeIndex(ref freeIndex) || !FindPreviousTakenIndex(ref takenIndex))
				{
					break;
				}
				if (freeIndex >= takenIndex)
				{
					break;
				}

				Entity entity = _indexEntity[takenIndex];

				_entityIndex[entity] = freeIndex;
				_indexEntity[takenIndex] = default;
				_indexEntity[freeIndex] = entity;

				CopyEntityFromTo(takenIndex, freeIndex);
			}
		}

		private void Resize(int newSize)
		{
			if (newSize == _storageSize) return;

			if (newSize < _storageSize)
			{
				if (newSize < _entityCount)
				{
					throw new Exception($"The new size ({newSize}) of the archetype storage is smaller then the current entity count ({_entityCount})");
				}
				CompactData();
			}

			void* oldData = (void*)_data;
			void* newData = (void*)AllocationHandler.Malloc(newSize * _archetypeSize);

			var bytesToCopy = newSize > _storageSize ? _storageSize * _archetypeSize : newSize * _archetypeSize;

			Buffer.MemoryCopy(oldData, newData, bytesToCopy, bytesToCopy);

			AllocationHandler.Free((IntPtr)oldData);
			_data = (IntPtr)newData;
			Array.Resize(ref _indexEntity, newSize);
			_storageSize = newSize;
		}

		public void Dispose()
		{
			AllocationHandler.Free(_data);
		}

		public class DebugView
		{
			private ArchetypeStorage _parent;

			public DebugView(ArchetypeStorage parent)
			{
				_parent = parent;
			}

			public (Entity entity, IComponent[] components)[] EntityComponents
			{
				get
				{
					(Entity entity, IComponent[] components)[] returnValue = new (Entity entity, IComponent[] components)[_parent._entityCount];
					int iteration = 0;
					foreach (var item in _parent._entityIndex)
					{
						returnValue[iteration].entity = item.Key;
						returnValue[iteration].components = new IComponent[_parent._archetype.ArchetypeComponents.Count];

						int componentIteration = 0;
						foreach (var component in _parent._archetype.ArchetypeComponents)
						{
							returnValue[iteration].components[componentIteration] = ComponentManager.CastPtrToIComponent(component, _parent.GetComponentPtr(item.Key, component));
							componentIteration++;
						}

						iteration++;
					}

					return returnValue;
				}
			}
		}

		internal unsafe class DirtyComponentQuery : IEnumerator<CurrentComponent>
		{
			//The enumerator and the entity index starts both with an invalid value
			private Identification[] _archetypeComponents;
			private ulong[] _componentOffsets;
			private ulong[] _dirtyOffsets;
			private ulong _componentCount;

			private Entity[] _entityIndexes;
			private ulong _entityCapacity;
			private byte* _data;
			private ulong _archetypeSize;

			private ulong _currentComponentIndex = 0;
			private ulong _currentEntityIndex = 0;
			private ulong _currentComponentOffset;
			private ulong _currentComponentDirtyOffset;
			private ulong _combinedComponentOffset;
			private byte* _currentDirtyPtr;
			private byte* _currentCmpPtr;

			public DirtyComponentQuery(ArchetypeStorage parent)
			{
				_archetypeComponents = new Identification[parent._archetype.ArchetypeComponents.Count];
				_componentOffsets = new ulong[_archetypeComponents.Length];
				_dirtyOffsets = new ulong[_archetypeComponents.Length];
				_componentCount = (ulong)_archetypeComponents.Length;
				_entityIndexes = parent._indexEntity;
				_entityCapacity = (ulong)_entityIndexes.Length;
				_archetypeSize = (ulong)parent._archetypeSize;
				_data = (byte*)parent._data;

				int i = 0;
				foreach (var component in parent._archetype.ArchetypeComponents)
				{
					_archetypeComponents[i] = component;
					_componentOffsets[i] = (ulong)parent._componentOffsets[component];
					_dirtyOffsets[i] = (ulong)ComponentManager.GetDirtyOffset(component);

					i++;
				}

				SetNextComponentData();
			}

			private void SetNextComponentData()
			{
				_currentComponentOffset = _componentOffsets[_currentComponentIndex];
				_currentComponentDirtyOffset = _dirtyOffsets[_currentComponentIndex];
				_currentDirtyPtr = _data + _currentComponentOffset + _currentComponentDirtyOffset;
				_currentCmpPtr = _data + _currentComponentOffset;

				_currentEntityIndex = 0;
				if (_entityIndexes[_currentEntityIndex].ArchetypeID.numeric == 0)
					FindNextEntity();
			}

			public CurrentComponent Current
			{
				get
				{
					return new CurrentComponent
					{
						ComponentID = _archetypeComponents[_currentComponentIndex],
						Entity = _entityIndexes[_currentEntityIndex],
						ComponentPtr = new IntPtr(_currentCmpPtr)
					};
				}
			}

			object IEnumerator.Current => Current;

			public void Dispose()
			{

			}

			/// <summary>
			/// Move the Iterator to the next Component marked as dirty and removes the dirty flag
			/// Dont reset the dirty flag, as this will generate an endless loop
			/// </summary>
			/// <returns>True if a component was found which is marked as dirty</returns>
			public bool MoveNext()
			{
				if (_currentEntityIndex < _entityCapacity && _entityIndexes[_currentEntityIndex].ArchetypeID.numeric == 0)
					FindNextEntity();
				while (!CurrentDirty())
				{
					if (!FindNextEntity() && !NextComponent())
					{
						return false;
					}
				}

				UnsetDirty();
				return true;
			}

			[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
			private bool NextComponent()
			{
				_currentComponentIndex++;
				if (ComponentIndexValid())
				{
					SetNextComponentData();
					return true;
				}
				return false;
			}

			[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
			private void UnsetDirty()
			{
				*_currentDirtyPtr = 0;
			}

			[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
			private bool CurrentDirty()
			{
				unchecked
				{
					return *_currentDirtyPtr != 0;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
			private bool ComponentIndexValid()
			{
				return _currentComponentIndex < _componentCount;
			}

			[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
			private bool EntityIndexValid()
			{
				return _currentEntityIndex < _entityCapacity;
			}

			private bool FindNextEntity()
			{
				ulong lastEntityIndex = _currentEntityIndex;
				do
				{
					_currentEntityIndex++;
					if (_currentEntityIndex >= _entityCapacity)
					{
						return false;
					}
				}
				while (_entityIndexes[_currentEntityIndex].ArchetypeID.numeric == 0);

				ulong offset = (_currentEntityIndex - lastEntityIndex) * _archetypeSize;
				_currentCmpPtr += offset;
				_currentDirtyPtr += offset;

				return true;
			}

			public void Reset()
			{
				throw new NotSupportedException();
			}
		}

		internal struct CurrentComponent
		{
			public Entity Entity;
			public Identification ComponentID;
			public IntPtr ComponentPtr;
		}
	}
}
