using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.SystemGroups
{
	[ExecuteInSystemGroup(typeof(SimulationSystemGroup))]
	class PhysicSystemGroup : ASystemGroup
	{
		public override Identification Identification => SystemGroupIDs.Physic;

		private float _accumulatedDeltaTime = 0f;

		public override Task QueueSystem(IEnumerable<Task> dependency)
		{
			List<Task> systemTasks = new();
			_accumulatedDeltaTime += MintyCore.DeltaTime;
			while(_accumulatedDeltaTime >= MintyCore.FixedDeltaTime)
			{
				systemTasks.Add(base.QueueSystem(dependency));
				_accumulatedDeltaTime -= MintyCore.FixedDeltaTime;
			}

			return Task.WhenAll(systemTasks);
		}
	}
}
