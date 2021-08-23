using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Utils;

namespace MintyCore.ECS
{
	abstract class ASystemGroup : ASystem
	{
		internal Dictionary<Identification, ASystem> _systems = new();

		public override void Setup()
		{
			var childSystemIDs = SystemManager._systemsPerSystemGroup[Identification];

			foreach (var systemID in childSystemIDs)
			{
				_systems.Add(systemID, SystemManager._systemCreateFunctions[systemID](World));
				_systems[systemID].Setup();
			}
		}

		public override void Dispose() { }
		public override void Execute() { }



		public override void PostExecuteMainThread()
		{
			foreach (var system in _systems)
			{
				system.Value.PostExecuteMainThread();
			}
		}

		public override Task QueueSystem(IEnumerable<Task> dependency)
		{
			List<Task> systemTaskCollection = new();

			var systemsToProcess = new Dictionary<Identification, ASystem>(_systems);
			var systemTasks = new Dictionary<Identification, Task>();

			while (systemsToProcess.Count > 0)
			{
				var systemsCopy = new Dictionary<Identification, ASystem>(systemsToProcess);

				foreach (var systemWithID in systemsCopy)
				{
					var id = systemWithID.Key;
					var system = systemWithID.Value;

					//Check if system is active
					if (World.SystemManager._inactiveSystems.Contains(id))
					{
						systemsToProcess.Remove(id);
						continue;
					}

					//Check if all required systems are executed
					bool missingDependency = false;
					foreach (var systemDepsID in SystemManager._executeSystemAfter[id])
					{
						if (systemsToProcess.ContainsKey(systemDepsID))
						{
							missingDependency = true;
							break;
						}
					}
					if (missingDependency)
					{
						continue;
					}


					List<Task> systemDependency = new();
					//Collect all needed JobHandles for the systemDependency
					foreach (var component in SystemManager._systemReadComponents[id])
					{
						if (World.SystemManager.SystemComponentAccess[component].accessType == ComponentAccessType.Write)
						{
							systemDependency.Add(World.SystemManager.SystemComponentAccess[component].task);
						}
					}
					foreach (var component in SystemManager._systemWriteComponents[id])
					{
						systemDependency.Add(World.SystemManager.SystemComponentAccess[component].task);
					}
					foreach (var systemDepsID in SystemManager._executeSystemAfter[id])
					{
						systemDependency.Add(systemTasks[systemDepsID]);
					}

					system.PreExecuteMainThread();
					var systemTask = system.QueueSystem(systemDependency);
					systemTaskCollection.Add(systemTask);
					systemTasks[id] = systemTask;

					foreach (var component in SystemManager._systemReadComponents[id])
					{
						if (World.SystemManager.SystemComponentAccess[component].accessType == ComponentAccessType.Read)
						{
							(var accessType, var task) = World.SystemManager.SystemComponentAccess[component];
							World.SystemManager.SystemComponentAccess[component] = (accessType, Task.WhenAll(task, systemTask));
							continue;
						}
						(ComponentAccessType, Task) componentAccess = new(ComponentAccessType.Read, systemTask);
						World.SystemManager.SystemComponentAccess[component] = componentAccess;
					}
					foreach (var component in SystemManager._systemWriteComponents[id])
					{
						(ComponentAccessType, Task) componentAccess = new(ComponentAccessType.Write, systemTask);
						World.SystemManager.SystemComponentAccess[component] = componentAccess;
					}

					systemsToProcess.Remove(id);
				}
			}

			return Task.WhenAll(systemTaskCollection);

		}
	}
}
