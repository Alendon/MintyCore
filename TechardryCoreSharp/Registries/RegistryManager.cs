using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.Registries
{
	public static class RegistryManager
	{
		private static Dictionary<ushort, IRegistry> _registries;

		private static Dictionary<string, ushort> _modID;
		private static Dictionary<string, ushort> _categoryID;
		private static Dictionary<Identification, Dictionary<string, uint>> _objectID;

		private static Dictionary<ushort, string> _reversedModID;
		private static Dictionary<ushort, string> _reversedCategoryID;
		private static Dictionary<Identification, Dictionary<uint, string>> _reversedObjectID;

		public static bool RegistryPhase { get; internal set; }

		public static ushort RegisterModID( string stringIdentifier )
		{
			if ( !RegistryPhase )
			{
				throw new Exception( "Game is not in registry phase" );
			}

			if ( _modID.ContainsKey( stringIdentifier ) )
			{
				return _modID[stringIdentifier];
			}

			ushort modID = Constants.InvalidID;
			do
			{
				modID++;
			} while ( _reversedModID.ContainsKey( modID ) );

			_modID.Add( stringIdentifier, modID );
			_reversedModID.Add( modID, stringIdentifier );

			return modID;
		}

		public static ushort RegisterCategoryID( string stringIdentifier )
		{
			if ( !RegistryPhase )
			{
				throw new Exception( "Game is not in registry phase" );
			}

			if ( _categoryID.ContainsKey( stringIdentifier ) )
			{
				return _categoryID[stringIdentifier];
			}

			ushort categoryID = Constants.InvalidID;
			do
			{
				categoryID++;
			} while ( _reversedCategoryID.ContainsKey( categoryID ) );

			_categoryID.Add( stringIdentifier, categoryID );
			_reversedCategoryID.Add( categoryID, stringIdentifier );

			return categoryID;
		}

		public static Identification RegisterObjectID( ushort modID, ushort categoryID, string stringIdentifier )
		{
			if ( !RegistryPhase )
			{
				throw new Exception( "Game is not in registry phase" );
			}

			Identification modCategoryID = new Identification( modID, categoryID, Constants.InvalidID );

			if ( !_objectID.ContainsKey( modCategoryID ) )
			{
				_objectID.Add( modCategoryID, new Dictionary<string, uint>() );
				_reversedObjectID.Add( modCategoryID, new Dictionary<uint, string>() );
			}

			if ( _objectID[modCategoryID].ContainsKey( stringIdentifier ) )
			{
				return new Identification( modID, categoryID, _objectID[modCategoryID][stringIdentifier] );
			}

			uint objectID = Constants.InvalidID;
			do
			{
				objectID++;
			} while ( _reversedObjectID[modCategoryID].ContainsKey( objectID ) );

			_objectID[modCategoryID].Add( stringIdentifier, objectID );
			_reversedObjectID[modCategoryID].Add( objectID, stringIdentifier );

			return new Identification( modID, categoryID, objectID );
		}

		public static void RegisterRegistry<T>( ushort categoryID ) where T : IRegistry, new()
		{
			if ( !RegistryPhase )
			{
				throw new Exception( "Game is not in registry phase" );
			}
			_registries.Add( categoryID, new T() );
		}

		internal static void ProcessRegistries()
		{
			foreach ( var registry in _registries )
			{
				foreach ( var dependency in registry.Value.RequiredRegistries )
				{
					if ( !_registries.ContainsKey( dependency ) )
					{
						throw new Exception( $"Registry'{_reversedCategoryID[registry.Key]}' depends on not present registry '{_reversedCategoryID[dependency]}'" );
					}
				}
			}


			Queue<IRegistry> registryOrder = new Queue<IRegistry>( _registries.Count );

			HashSet<IRegistry> registriesToProcess = new HashSet<IRegistry>( _registries.Values );

			while ( registryOrder.Count > 0 )
			{
				foreach ( var registry in new HashSet<IRegistry>( registriesToProcess ) )
				{
					bool allDependenciesPresent = true;
					foreach ( var dependency in registry.RequiredRegistries )
					{
						if ( registriesToProcess.Contains( _registries[dependency] ) )
						{
							allDependenciesPresent = false;
							break;
						}
					}

					if ( !allDependenciesPresent )
					{
						continue;
					}

					registryOrder.Enqueue( registry );
					registriesToProcess.Remove( registry );
				}
			}

			for ( Queue<IRegistry> registries = new Queue<IRegistry>( registryOrder ); registries.Count > 0; )
			{
				registries.Dequeue().PreRegister();
			}

			for ( Queue<IRegistry> registries = new Queue<IRegistry>( registryOrder ); registries.Count > 0; )
			{
				registries.Dequeue().Register();
			}

			for ( Queue<IRegistry> registries = new Queue<IRegistry>( registryOrder ); registries.Count > 0; )
			{
				registries.Dequeue().PostRegister();
			}
		}
	}
}
