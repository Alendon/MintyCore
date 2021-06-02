using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;
using MintyCore.Utils.JobSystem;

namespace MintyCore.ECS
{
	abstract class ASystemGroup : ASystem
	{
		internal Dictionary<Identification, ASystem> _systems = new Dictionary<Identification, ASystem>();

		public override bool ExecuteOnMainThread => true;

		public override void Setup()
		{
			var childSystemIDs = SystemManager._systemsPerSystemGroup[Identification];

			foreach ( var systemID in childSystemIDs )
			{
				_systems.Add( systemID, SystemManager._systemCreateFunctions[systemID](World) );
				_systems[systemID].Setup();
			}
		}

		public override void Dispose() { }
		public override void Execute() => throw new NotImplementedException();

		 

		public override void PostExecuteMainThread()
		{
			foreach ( var system in _systems )
			{
				system.Value.PostExecuteMainThread();
			}
		}

		public override JobHandleCollection QueueSystem( JobHandleCollection dependency )
		{
			JobHandleCollection systemHandleCollection = new JobHandleCollection();

			var systemsToProcess = new Dictionary<Identification, ASystem>( _systems );
			var systemJobHandles = new Dictionary<Identification, JobHandleCollection>();

			while ( systemsToProcess.Count > 0 )
			{
				var systemsCopy = new Dictionary<Identification, ASystem>( systemsToProcess );

				foreach ( var systemWithID in systemsCopy )
				{
					var id = systemWithID.Key;
					var system = systemWithID.Value;

					//Check if system is active
					if ( World.SystemManager._inactiveSystems.Contains( id ) )
					{
						systemsToProcess.Remove( id );
						continue;
					}

					//Check if all required systems are executed
					bool missingDependency = false;
					foreach ( var systemDepsID in SystemManager._executeSystemAfter[id] )
					{
						if ( systemsToProcess.ContainsKey( systemDepsID ) )
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
					foreach ( var component in SystemManager._systemReadComponents[id] )
					{
						if ( World.SystemManager.SystemComponentAccess[component].Key == ComponentAccessType.Write )
						{
							systemDependency.Merge( World.SystemManager.SystemComponentAccess[component].Value );
						}
					}
					foreach ( var component in SystemManager._systemWriteComponents[id] )
					{
						systemDependency.Merge( World.SystemManager.SystemComponentAccess[component].Value );
					}
					foreach ( var systemDepsID in SystemManager._executeSystemAfter[id] )
					{
						systemDependency.Merge( systemJobHandles[systemDepsID] );
					}

					system.PreExecuteMainThread();
					var systemJobHandle = system.QueueSystem( systemDependency );
					systemHandleCollection.Merge( systemJobHandle );
					systemJobHandles[id] = systemJobHandle;

					foreach ( var component in SystemManager._systemReadComponents[id] )
					{
						if ( World.SystemManager.SystemComponentAccess[component].Key == ComponentAccessType.Read )
						{
							World.SystemManager.SystemComponentAccess[component].Value.Merge( systemJobHandle );
							continue;
						}
						KeyValuePair<ComponentAccessType, JobHandleCollection> componentAccess = new KeyValuePair<ComponentAccessType, JobHandleCollection>( ComponentAccessType.Read, systemJobHandle );
						World.SystemManager.SystemComponentAccess[component] = componentAccess;
					}
					foreach ( var component in SystemManager._systemWriteComponents[id] )
					{
						KeyValuePair<ComponentAccessType, JobHandleCollection> componentAccess = new KeyValuePair<ComponentAccessType, JobHandleCollection>( ComponentAccessType.Write, systemJobHandle );
						World.SystemManager.SystemComponentAccess[component] = componentAccess;
					}

					systemsToProcess.Remove( id );
				}
			}

			return systemHandleCollection;

		}
	}
}
