using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Components.Common;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.ECS
{
	public class ComponentQuery : IEnumerable<ComponentQuery.CurrentEntity>
	{
		private HashSet<Identification> _usedComponents = new HashSet<Identification>();
		private HashSet<Identification> _readOnlyComponents = new HashSet<Identification>();
		private HashSet<Identification> _excludeComponents = new HashSet<Identification>();

		Dictionary<Identification, ArchetypeStorage> _archetypeStorages;

		public void Setup(ASystem system)
		{
			var archetypeMap = ArchetypeManager.GetArchetypes();

			foreach ( var entry in archetypeMap )
			{
				var id = entry.Key;
				var archetype = entry.Value;

				bool containsAllComponents = true;
				foreach(var component in _usedComponents )
				{
					if ( !archetype.ArchetypeComponents.Contains( component ) )
					{
						containsAllComponents = false;
						break;
					}
				}

				if ( !containsAllComponents )
				{
					continue;
				}

				bool containsNoExcludeComponents = true;
				foreach ( var component in _excludeComponents )
				{
					if ( archetype.ArchetypeComponents.Contains( component ) )
					{
						containsNoExcludeComponents = false;
						break;
					}
				}

				if ( !containsNoExcludeComponents )
				{
					continue;
				}

				_archetypeStorages.Add( id, EntityManager.GetArchetypeStorage( id ) );
			}

			_excludeComponents.Clear();

			SystemManager.SetReadComponents( system.Identification, _readOnlyComponents );
			SystemManager.SetWriteComponents( system.Identification, _usedComponents.Except( _readOnlyComponents ) );
		}

		public void WithComponents( params Identification[] componentID )
		{
			_usedComponents.UnionWith( componentID );
		}

		public void WithReadOnlyComponents( params Identification[] componentID )
		{
			_usedComponents.UnionWith( componentID );
			_readOnlyComponents.UnionWith( componentID );
		}

		public void ExcludeComponents( params Identification[] componentID )
		{
			_excludeComponents = new HashSet<Identification>( componentID );
		}

		public IEnumerator<CurrentEntity> GetEnumerator() => new Enumerator( this, _usedComponents, _readOnlyComponents );
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<CurrentEntity>
		{
			private ComponentQuery _parent;
			private CurrentEntity _current;

			private Dictionary<Identification, ArchetypeStorage>.Enumerator archetypeEnumerator;
			private Dictionary<Entity, int>.Enumerator entityEnumerator;

			public Enumerator( ComponentQuery parent, HashSet<Identification> usedComponents, HashSet<Identification> readOnlyComponents )
			{
				_parent = parent;
				_current = default;
				_current._usedComponents = usedComponents;
				_current._readOnlyComponents = readOnlyComponents;

				archetypeEnumerator = _parent._archetypeStorages.GetEnumerator();
				entityEnumerator = default;
			}

			public CurrentEntity Current => _current;

			object IEnumerator.Current => Current;

			public void Dispose() => archetypeEnumerator.Dispose();
			public bool MoveNext()
			{
				while ( true )
				{
					if ( entityEnumerator.MoveNext() )
					{
						_current.Entity = entityEnumerator.Current.Key;
						return true;
					}
					if ( archetypeEnumerator.MoveNext() )
					{
						_current._currentStorage = archetypeEnumerator.Current.Value;
						entityEnumerator = _current._currentStorage._entityIndex.GetEnumerator();
						continue;
					}
					return false;
				}
			}

			public void Reset()
			{
				_current.Entity = default;
				archetypeEnumerator = _parent._archetypeStorages.GetEnumerator();
				entityEnumerator = default;
			}
		}

		public struct CurrentEntity
		{
			public Entity Entity;
			internal ArchetypeStorage _currentStorage;
			internal HashSet<Identification> _readOnlyComponents;
			internal HashSet<Identification> _usedComponents;

			public ref Component GetComponent<Component>( Identification id ) where Component : unmanaged, IComponent
			{
				return ref _currentStorage.GetRefComponent<Component>( Entity, id );
			}

			public Component GetReadOnlyComponent<Component>( Identification id ) where Component : unmanaged, IComponent
			{
				return _currentStorage.GetComponent<Component>( Entity, id );
			}

			public ref Component GetComponent<Component>() where Component : unmanaged, IComponent
			{
				return ref _currentStorage.GetRefComponent<Component>( Entity );
			}

			public Component GetReadOnlyComponent<Component>() where Component : unmanaged, IComponent
			{
				return _currentStorage.GetComponent<Component>( Entity );
			}
		}
	}
}
