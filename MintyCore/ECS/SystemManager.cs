using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using MintyCore.Utils.JobSystem;

namespace MintyCore.ECS
{
	/// <summary>
	/// Specify that a system will be executed after one or multiple others
	/// </summary>
	[AttributeUsage( AttributeTargets.Class, AllowMultiple = false )]
	public class ExecuteAfterAttribute : Attribute
	{
		internal Type[] ExecuteAfter { get; private set; }

		/// <summary>
		/// Specify that the system will be executed after <paramref name="executeAfter"/>
		/// </summary>
		public ExecuteAfterAttribute( params Type[] executeAfter )
		{
			foreach ( var type in executeAfter )
			{
				if ( Activator.CreateInstance( type ) is not ASystem )
				{
					throw new ArgumentException( "Types used with the ExecuteAfterAttribute have to be Assignable from ASystem" );
				}
			}
			ExecuteAfter = executeAfter;
		}
	}

	/// <summary>
	/// Specify that a system will be executed before one or multiple others
	/// </summary>
	[AttributeUsage( AttributeTargets.Class, AllowMultiple = false )]
	public class ExecuteBeforeAttribute : Attribute
	{
		internal Type[] ExecuteBefore { get; private set; }

		/// <summary>
		/// Specify that the system will be executed after <paramref name="executeBefore"/>
		/// </summary>
		public ExecuteBeforeAttribute( params Type[] executeBefore )
		{
			foreach ( var type in executeBefore )
			{
				if ( Activator.CreateInstance( type ) is not ASystem )
				{
					throw new ArgumentException( "Types used with the ExecuteBeforeAttribute have to be Assignable from ASystem" );
				}
			}
			ExecuteBefore = executeBefore;
		}
	}

	/// <summary>
	/// Specify the SystemGroup the system will be executed in. If the attribute is not applied, the system will be executed in <see cref="SimulationSystemGroup"/>
	/// </summary>
	[AttributeUsage( AttributeTargets.Class, AllowMultiple = false )]
	public class ExecuteInSystemGroupAttribute : Attribute
	{
		internal Type SystemGroup { get; private set; }

		/// <summary/>
		public ExecuteInSystemGroupAttribute( Type systemGroup )
		{
			if ( Activator.CreateInstance( systemGroup ) is not ASystemGroup )
			{
				throw new ArgumentException( "Type used with the SystemGroupAttribute have to be Assignable from ASystem" );
			}
			SystemGroup = systemGroup;
		}
	}

	/// <summary>
	/// Specify that this SystemGroup is a RootSystemGroup (this system group does not have a parent system group)
	/// </summary>
	[AttributeUsage( AttributeTargets.Class, AllowMultiple = false )]
	public class RootSystemGroupAttribute : Attribute
	{

	}

	/// <summary>
	/// Specify the ExecutionSide of a system
	/// </summary>
	[AttributeUsage( AttributeTargets.Class, AllowMultiple = false )]
	public class ExecutionSideAttribute : Attribute
	{
		/// <summary>
		/// 
		/// </summary>
		public GameType ExecutionSide;

		/// <summary>
		/// Specify the ExecutionSide of a system
		/// </summary>
		public ExecutionSideAttribute( GameType executionSide )
		{
			ExecutionSide = executionSide;
		}
	}

	/// <summary>
	/// The <see cref="SystemManager"/> contains all system handling stuff (populated by <see cref="Registries.SystemRegistry"/> and manages the systems for a <see cref="World"/>
	/// </summary>
	public class SystemManager
	{
		#region static setup stuff
		internal static Dictionary<Identification, Func<World, ASystem>> _systemCreateFunctions = new Dictionary<Identification, Func<World, ASystem>>();

		internal static Dictionary<Identification, HashSet<Identification>> _systemReadComponents = new Dictionary<Identification, HashSet<Identification>>();
		internal static Dictionary<Identification, HashSet<Identification>> _systemWriteComponents = new Dictionary<Identification, HashSet<Identification>>();

		internal static HashSet<Identification> _rootSystemGroupIDs = new HashSet<Identification>();
		internal static Dictionary<Identification, HashSet<Identification>> _systemsPerSystemGroup = new Dictionary<Identification, HashSet<Identification>>();
		internal static Dictionary<Identification, HashSet<Identification>> _executeSystemAfter = new Dictionary<Identification, HashSet<Identification>>();
		internal static Dictionary<Identification, GameType> _systemExecutionSide = new Dictionary<Identification, GameType>();

		internal static HashSet<Identification> _systemsToSort = new HashSet<Identification>();
		internal static Dictionary<Identification, Identification> _systemGroupPerSystem = new Dictionary<Identification, Identification>();

		internal static void Clear()
		{
			_systemCreateFunctions.Clear();
			_systemReadComponents.Clear();
			_systemWriteComponents.Clear();
			_rootSystemGroupIDs.Clear();
			_systemsPerSystemGroup.Clear();
			_executeSystemAfter.Clear();
			_systemExecutionSide.Clear();
			_systemsToSort.Clear();
			_systemGroupPerSystem.Clear();
		}

		internal static void SetReadComponents( Identification systemID, HashSet<Identification> readComponents )
		{
			_systemReadComponents[systemID].UnionWith( readComponents );

			//Check if the current system is a root system group
			if ( !_rootSystemGroupIDs.Contains( systemID ) )
			{
				//Recursive call with the parent SystemGroup
				SetReadComponents( _systemGroupPerSystem[systemID], readComponents );
			}
		}

		internal static void SetWriteComponents( Identification systemID, HashSet<Identification> writeComponents )
		{
			_systemWriteComponents[systemID].UnionWith( writeComponents );

			//Check if the current system is a root system group
			if ( !_rootSystemGroupIDs.Contains( systemID ) )
			{
				//Recursive call with the parent SystemGroup
				SetWriteComponents( _systemGroupPerSystem[systemID], writeComponents );
			}
		}

		internal static void RegisterSystem<System>( Identification systemID ) where System : ASystem, new()
		{
			_systemCreateFunctions.Add( systemID, ( World world ) =>
			 {
				 System system = new System();
				 system.World = world;
				 return system;
			 } );
			_systemsToSort.Add( systemID );

			_systemWriteComponents.Add( systemID, new HashSet<Identification>() );
			_systemReadComponents.Add( systemID, new HashSet<Identification>() );
			_executeSystemAfter.Add( systemID, new HashSet<Identification>() );
		}

		private static void ValidateExecuteAfter( Identification systemID, Identification afterSystemID )
		{
			bool isSystemRoot = _rootSystemGroupIDs.Contains( systemID );
			bool isToExecuteAfterRoot = _rootSystemGroupIDs.Contains( afterSystemID );

			if ( isSystemRoot && isToExecuteAfterRoot )
			{
				return;
			}

			if ( isSystemRoot != isToExecuteAfterRoot )
			{
				throw new Exception( "Systems to execute after have to be either in the same group or be both a root system group" );
			}

			if ( _systemGroupPerSystem[afterSystemID] != _systemGroupPerSystem[systemID] )
			{
				throw new Exception( "Systems to execute after have to be either in the same group or be both a root system group" );
			}

		}

		private static void ValidateExecuteBefore( Identification systemID, Identification beforeSystemID )
		{
			bool isSystemRoot = _rootSystemGroupIDs.Contains( systemID );
			bool isToExecuteBeforeRoot = _rootSystemGroupIDs.Contains( beforeSystemID );

			if ( isSystemRoot && isToExecuteBeforeRoot )
			{
				return;
			}

			if ( isSystemRoot != isToExecuteBeforeRoot )
			{
				throw new Exception( "Systems to execute before have to be either in the same group or be both a root system group" );
			}

			if ( _systemGroupPerSystem[beforeSystemID] != _systemGroupPerSystem[systemID] )
			{
				throw new Exception( "Systems to execute before have to be either in the same group or be both a root system group" );
			}
		}

		internal static void SortSystems()
		{
			Dictionary<Identification, ASystem> systemInstances = new Dictionary<Identification, ASystem>();
			Dictionary<Identification, Type> systemTypes = new Dictionary<Identification, Type>();
			Dictionary<Type, Identification> reversedSystemTypes = new Dictionary<Type, Identification>();

			//Populate helper dictionaries
			foreach ( var systemID in _systemsToSort )
			{
				ASystem system = _systemCreateFunctions[systemID]( null );
				Type systemType = system.GetType();

				systemInstances.Add( systemID, system );
				systemTypes.Add( systemID, systemType );
				reversedSystemTypes.Add( systemType, systemID );
			}

			//Detect SystemGroups
			Type rootSystemGroupType = typeof( RootSystemGroupAttribute );
			foreach ( var systemID in _systemsToSort )
			{
				if ( systemInstances[systemID] is not ASystemGroup )
				{
					continue;
				}

				if ( Attribute.IsDefined( systemTypes[systemID], rootSystemGroupType ) )
				{
					_rootSystemGroupIDs.Add( systemID );
				}

				_systemsPerSystemGroup.Add( systemID, new HashSet<Identification>() );
			}

			//Sort systems into SystemGroups
			Type executeInSystemGroupType = typeof( ExecuteInSystemGroupAttribute );

			foreach ( var systemID in _systemsToSort )
			{
				if ( _rootSystemGroupIDs.Contains( systemID ) )
				{
					continue;
				}


				if (Attribute.GetCustomAttribute(systemTypes[systemID], executeInSystemGroupType) is not ExecuteInSystemGroupAttribute executeInSystemGroup)
				{
					_systemsPerSystemGroup[SystemGroupIDs.Simulation].Add(systemID);
					_systemGroupPerSystem.Add(systemID, SystemGroupIDs.Simulation);
					continue;
				}

				Identification systemGroupID = reversedSystemTypes[executeInSystemGroup.SystemGroup];

				_systemsPerSystemGroup[systemGroupID].Add( systemID );
				_systemGroupPerSystem.Add( systemID, systemGroupID );
			}

			//Sort execution order
			Type executeAfterType = typeof( ExecuteAfterAttribute );
			Type executeBeforeType = typeof( ExecuteBeforeAttribute );
			foreach ( var systemID in _systemsToSort )
			{
				ExecuteAfterAttribute executeAfter = Attribute.GetCustomAttribute( systemTypes[systemID], executeAfterType ) as ExecuteAfterAttribute;
				ExecuteBeforeAttribute executeBefore = Attribute.GetCustomAttribute( systemTypes[systemID], executeBeforeType ) as ExecuteBeforeAttribute;

				if ( executeAfter is not null )
				{
					if ( !_executeSystemAfter.ContainsKey( systemID ) )
					{
						_executeSystemAfter.Add( systemID, new HashSet<Identification>() );
					}

					foreach ( var afterSystemType in executeAfter.ExecuteAfter )
					{
						if ( !reversedSystemTypes.ContainsKey( afterSystemType ) )
						{
							throw new Exception( $"The system to execute after is not present" );
						}
						var afterSystemID = reversedSystemTypes[afterSystemType];

						ValidateExecuteAfter( systemID, afterSystemID );

						_executeSystemAfter[systemID].Add( afterSystemID );
					}
				}

				if ( executeBefore is not null )
				{
					foreach ( var beforeSystemType in executeBefore.ExecuteBefore )
					{
						if ( !reversedSystemTypes.ContainsKey( beforeSystemType ) )
						{
							throw new Exception( $"The system to execute before is not present" );
						}
						var beforeSystemID = reversedSystemTypes[beforeSystemType];

						ValidateExecuteBefore( systemID, beforeSystemID );

						_executeSystemAfter[beforeSystemID].Add( systemID );

					}
				}

			}

			//Sort execution side (client, server, both)
			Type executionSideType = typeof( ExecutionSideAttribute );
			foreach ( var systemID in _systemsToSort )
			{
				if (Attribute.GetCustomAttribute(systemTypes[systemID], executionSideType) is not ExecutionSideAttribute executionSide)
				{
					_systemExecutionSide.Add(systemID, GameType.Local);
					continue;
				}

				_systemExecutionSide.Add( systemID, executionSide.ExecutionSide );
			}

			_systemsToSort.Clear();
		}
		#endregion

		internal World _parent;
		internal HashSet<Identification> _inactiveSystems = new HashSet<Identification>();
		internal Dictionary<Identification, ASystem> _rootSystems = new Dictionary<Identification, ASystem>();

		internal Dictionary<Identification, KeyValuePair<ComponentAccessType, JobHandleCollection>> SystemComponentAccess = new Dictionary<Identification, KeyValuePair<ComponentAccessType, JobHandleCollection>>();

		/// <summary>
		/// Create a new SystemManager for <paramref name="world"/>
		/// </summary>
		/// <param name="world"></param>
		public SystemManager(World world )
		{
			_parent = world;

			foreach ( var systemID in _rootSystemGroupIDs )
			{
				_rootSystems.Add( systemID, _systemCreateFunctions[systemID](_parent) );
				_rootSystems[systemID].Setup();
			}

		}

		internal void Execute()
		{
			RePopulateSystemComponentAccess();

			JobHandleCollection systemHandleCollection = new JobHandleCollection();

			var rootSystemsToProcess = new Dictionary<Identification, ASystem>( _rootSystems );
			var systemJobHandles = new Dictionary<Identification, JobHandleCollection>();

			while ( rootSystemsToProcess.Count > 0 )
			{
				var systemsCopy = new Dictionary<Identification, ASystem>( rootSystemsToProcess );

				foreach ( var systemWithID in systemsCopy )
				{
					var id = systemWithID.Key;
					var system = systemWithID.Value;

					//Check if system is active
					if ( _inactiveSystems.Contains( id ) )
					{
						rootSystemsToProcess.Remove( id );
						continue;
					}

					//Check if all required systems are executed
					bool missingDependency = false;
					foreach ( var dependency in _executeSystemAfter[id] )
					{
						if ( rootSystemsToProcess.ContainsKey( dependency ) )
						{
							missingDependency = true;
							break;
						}
					}
					if ( missingDependency )
					{
						continue;
					}


					JobHandleCollection systemDependency = new JobHandleCollection();
					//Collect all needed JobHandles for the systemDependency
					foreach ( var component in _systemReadComponents[id] )
					{
						if ( SystemComponentAccess[component].Key == ComponentAccessType.Write )
						{
							systemDependency.Merge( SystemComponentAccess[component].Value );
						}
					}
					foreach ( var component in _systemWriteComponents[id] )
					{
						systemDependency.Merge( SystemComponentAccess[component].Value );
					}
					foreach ( var dependency in _executeSystemAfter[id] )
					{
						systemDependency.Merge( systemJobHandles[dependency] );
					}
					
					{
						system.PreExecuteMainThread();
					}

					var systemJobHandle = system.QueueSystem( systemDependency );
					systemHandleCollection.Merge( systemJobHandle );
					systemJobHandles[id] = systemJobHandle;

					foreach ( var component in _systemReadComponents[id] )
					{
						if ( SystemComponentAccess[component].Key == ComponentAccessType.Read )
						{
							SystemComponentAccess[component].Value.Merge( systemJobHandle );
							continue;
						}
						KeyValuePair<ComponentAccessType, JobHandleCollection> componentAccess = new KeyValuePair<ComponentAccessType, JobHandleCollection>( ComponentAccessType.Read, systemJobHandle );
						SystemComponentAccess[component] = componentAccess;
					}
					foreach ( var component in _systemWriteComponents[id] )
					{
						KeyValuePair<ComponentAccessType, JobHandleCollection> componentAccess = new KeyValuePair<ComponentAccessType, JobHandleCollection>( ComponentAccessType.Write, systemJobHandle );
						SystemComponentAccess[component] = componentAccess;
					}

					rootSystemsToProcess.Remove( id );
				}
			}


			//Wait for the completion of all systems
			systemHandleCollection.Complete();

			foreach ( var system in _rootSystems )
			{
				system.Value.PostExecuteMainThread();
			}
		}

		private void RePopulateSystemComponentAccess()
		{
			foreach ( Identification component in ComponentManager.GetComponentList() )
			{
				if ( !SystemComponentAccess.ContainsKey( component ) )
				{
					SystemComponentAccess.Add( component, new KeyValuePair<ComponentAccessType, JobHandleCollection>( ComponentAccessType.None, new JobHandleCollection() ) );
					continue;
				}
				SystemComponentAccess[component] = new KeyValuePair<ComponentAccessType, JobHandleCollection>( ComponentAccessType.None, new JobHandleCollection() );
			}
		}
	}

	enum ComponentAccessType
	{
		None = 0,
		Read,
		Write
	}
}
