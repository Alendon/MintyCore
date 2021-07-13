using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Components.Common;
using MintyCore.Utils;

namespace MintyCore.ECS
{
	public class ComponentQuery : IEnumerable<ComponentQuery.CurrentEntity>
	{
		private readonly HashSet<Identification> _usedComponents = new();
		private readonly HashSet<Identification> _readOnlyComponents = new();
		private HashSet<Identification> _excludeComponents = new();
		readonly Dictionary<Identification, ArchetypeStorage> _archetypeStorages = new();

		public void Setup(ASystem system)
		{
			var archetypeMap = ArchetypeManager.GetArchetypes();

			foreach (var entry in archetypeMap)
			{
				var id = entry.Key;
				var archetype = entry.Value;

				bool containsAllComponents = true;
				foreach (var component in _usedComponents)
				{
					if (!archetype.ArchetypeComponents.Contains(component))
					{
						containsAllComponents = false;
						break;
					}
				}

				if (!containsAllComponents)
				{
					continue;
				}

				bool containsNoExcludeComponents = true;
				foreach (var component in _excludeComponents)
				{
					if (archetype.ArchetypeComponents.Contains(component))
					{
						containsNoExcludeComponents = false;
						break;
					}
				}

				if (!containsNoExcludeComponents)
				{
					continue;
				}

				_archetypeStorages.Add(id, system.World.EntityManager.GetArchetypeStorage(id));
			}

			_excludeComponents.Clear();

			SystemManager.SetReadComponents(system.Identification, _readOnlyComponents);
			SystemManager.SetWriteComponents(system.Identification, new HashSet<Identification>(_usedComponents.Except(_readOnlyComponents)));
		}

		public void WithComponents(params Identification[] componentID)
		{
			_usedComponents.UnionWith(componentID);
		}

		public void WithReadOnlyComponents(params Identification[] componentID)
		{
			_usedComponents.UnionWith(componentID);
			_readOnlyComponents.UnionWith(componentID);
		}

		public void ExcludeComponents(params Identification[] componentID)
		{
			_excludeComponents = new HashSet<Identification>(componentID);
		}

		public IEnumerator<CurrentEntity> GetEnumerator() => new Enumerator(this, _usedComponents, _readOnlyComponents);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<CurrentEntity>
		{
			private readonly ComponentQuery _parent;
			private CurrentEntity _current;



			private Dictionary<Identification, ArchetypeStorage>.Enumerator _archetypeEnumerator;
			private Entity[] _entityIndexes;
			private int _entityIndex;

			public Enumerator(ComponentQuery parent, HashSet<Identification> usedComponents, HashSet<Identification> readOnlyComponents)
			{
				_parent = parent;
				_current = default;

				_current._componentIDs = new Identification[usedComponents.Count];
				_current._componentOffsets = new int[usedComponents.Count];

#if DEBUG
				_current._usedComponents = usedComponents;
				_current._readOnlyComponents = readOnlyComponents;
#endif

				_archetypeEnumerator = _parent._archetypeStorages.GetEnumerator();
				_entityIndexes = Array.Empty<Entity>();
				_entityIndex = -1;
			}

			public CurrentEntity Current => _current;

			object IEnumerator.Current => Current;

			public void Dispose() => _archetypeEnumerator.Dispose();
			public bool MoveNext()
			{
				do
				{
					if (!NextEntity() && !NextArchetype())
					{
						return false;
					}
				}
				while (!CurrentValid());

				_current.Entity = _entityIndexes[_entityIndex];
				_current.EntityIndex = _entityIndex;
				_current._componentIndex = 0;

				if (_current.NextArchetype && _entityIndex > 0)
					_current.NextArchetype = false;

				return true;
			}

			private bool NextEntity()
			{
				_entityIndex++;
				return EntityIndexValid();
			}

			private bool NextArchetype()
			{
				if (!_archetypeEnumerator.MoveNext())
				{
					return false;
				}
				_current._currentStorage = _archetypeEnumerator.Current.Value;
				_current.NextArchetype = true;

				_entityIndexes = _current._currentStorage._indexEntity;
				_entityIndex = -1;
				return true;
			}

			private bool EntityIndexValid()
			{
				return _entityIndex >= 0 && _entityIndex < _entityIndexes.Length;
			}

			private bool CurrentValid()
			{
				return EntityIndexValid() && _entityIndexes[_entityIndex] != default;
			}

			public void Reset()
			{
				throw new NotSupportedException();
			}
		}

		public struct CurrentEntity
		{
			public Entity Entity;
			internal int EntityIndex;

			internal ArchetypeStorage _currentStorage;

			internal Identification[] _componentIDs;
			internal int[] _componentOffsets;
			internal int _componentIndex;
			internal bool NextArchetype;

#if DEBUG
			internal HashSet<Identification> _readOnlyComponents;
			internal HashSet<Identification> _usedComponents;
#endif

			private unsafe Component* GetComponentPtr<Component>(Identification id) where Component : unmanaged, IComponent
			{
				bool orderChanged = false;
				if (_componentIDs[_componentIndex] != id)
				{
					_componentIDs[_componentIndex] = id;
					orderChanged = true;
				}
				if (NextArchetype || orderChanged)
				{
					_componentOffsets[_componentIndex] = _currentStorage._componentOffsets[id];
				}
				var ptr = (Component*)(_currentStorage._data + (EntityIndex * _currentStorage._archetypeSize) + _componentOffsets[_componentIndex]);
				_componentIndex++;
				return ptr;
			}

			public unsafe ref Component GetComponent<Component>(Identification id) where Component : unmanaged, IComponent
			{
#if DEBUG
				if (!_usedComponents.Contains(id))
				{
					throw new InvalidOperationException($"The {nameof(ComponentQuery)} was not created with the component {id}.");
				}
				if (_readOnlyComponents.Contains(id))
				{
					throw new InvalidOperationException($"The Component {id} is marked as readonly.");
				}
#endif
				return ref *GetComponentPtr<Component>(id);
			}

			public unsafe Component GetReadOnlyComponent<Component>(Identification id) where Component : unmanaged, IComponent
			{
#if DEBUG
				if (!_usedComponents.Contains(id))
				{
					throw new InvalidOperationException($"The {nameof(ComponentQuery)} was not created with the component {id}.");
				}
#endif
				return *GetComponentPtr<Component>(id);
			}

			public ref Component GetComponent<Component>() where Component : unmanaged, IComponent
			{
				Component component = default;
				return ref GetComponent<Component>(component.Identification);
			}

			public Component GetReadOnlyComponent<Component>() where Component : unmanaged, IComponent
			{
				Component component = default;
				return GetReadOnlyComponent<Component>(component.Identification);
			}
		}
	}



}
