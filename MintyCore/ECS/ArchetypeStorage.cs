using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Components;
using MintyCore.Utils;

namespace MintyCore.ECS
{

	[DebuggerTypeProxy(typeof(DebugView))]
	internal unsafe class ArchetypeStorage : IDisposable
	{
		private IntPtr _data;
		private readonly int _archetypeSize = 0;
		private readonly Dictionary<Identification, int> _componentOffsets = new();
		private ArchetypeContainer _archetype;

		private const int _defaultStorageSize = 16;
		private int _entityCount = 0;
		private int _storageSize = _defaultStorageSize;


		//Key: Entity, Value: Index
		internal Dictionary<Entity, int> _entityIndex = new Dictionary<Entity, int>(_defaultStorageSize);

		//Index (of Array): Index (in Memory), Value: Entity
		internal Entity[] _indexEntity = new Entity[_defaultStorageSize];

		private int entityIndexSearchPivot = -1;


		internal ArchetypeStorage(ArchetypeContainer archetype)
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
			return _data + (entityIndex *_archetypeSize) + _componentOffsets[componentID];
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

		internal class DirtyComponentQuery : IEnumerator<CurrentComponent>
		{
			private ArchetypeStorage _parent;

			//The enumerator and the entity index starts both with an invalid value
			private Identification[] _archetypeComponents;
			private int[] _componentOffsets;
			private int[] _dirtyOffsets;

			private int _currentComponentIndex = -1;
			private int _currentEntityIndex = -1;

			public DirtyComponentQuery(ArchetypeStorage parent)
			{
				_parent = parent;
				_archetypeComponents = new Identification[parent._archetype.ArchetypeComponents.Count];
				_componentOffsets = new int[_archetypeComponents.Length];
				_dirtyOffsets = new int[_archetypeComponents.Length];

				int i = 0;
				foreach (var component in parent._archetype.ArchetypeComponents)
				{
					_archetypeComponents[i] = component;
					_componentOffsets[i] = _parent._componentOffsets[component];
					_dirtyOffsets[i] = ComponentManager.GetDirtyOffset(component);

					i++;
				}
			}

			public CurrentComponent Current
			{
				get
				{
					return new CurrentComponent
					{
						ComponentID = _archetypeComponents[_currentComponentIndex],
						Entity = _parent._indexEntity[_currentEntityIndex],
						ComponentPtr = _parent._data + (_parent._archetypeSize * _currentEntityIndex) + _componentOffsets[_currentComponentIndex]
					};
				}
			}

			object IEnumerator.Current => Current;

			public void Dispose()
			{

			}

			public bool MoveNext()
			{
				do
				{
					if (!NextComponent() && !FindNextEntity())
					{
						return false;
					}
				}
				while (!CurrentValid());

				return true;
			}

			private bool NextComponent()
			{
				_currentComponentIndex++;
				return ComponentIndexValid();
			}

			private bool CurrentValid()
			{
				return ComponentIndexValid() && EntityIndexValid() && CurrentDirty();
			}

			private bool CurrentDirty()
			{
				return *((byte*)_parent._data + (_parent._archetypeSize * _currentEntityIndex) + (_componentOffsets[_currentComponentIndex]) + _dirtyOffsets[_currentComponentIndex]) != 0;
			}

			private bool ComponentIndexValid()
			{
				return _currentComponentIndex >= 0 && _currentComponentIndex < _archetypeComponents.Length;
			}

			private bool EntityIndexValid()
			{
				return _currentEntityIndex >= 0 && _currentEntityIndex < _parent._indexEntity.Length;
			}

			private bool FindNextEntity()
			{
				do
				{
					_currentEntityIndex++;
					if (_currentEntityIndex >= _parent._indexEntity.Length)
					{
						return false;
					}
				}
				while (_parent._indexEntity[_currentEntityIndex] == default);

				_currentComponentIndex = -1;

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
