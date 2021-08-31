using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.SystemGroups
{
    [ExecuteInSystemGroup(typeof(SimulationSystemGroup))]
    internal class PhysicSystemGroup : ASystemGroup
    {
        private float _accumulatedDeltaTime;
        public override Identification Identification => SystemGroupIDs.Physic;

        public override Task QueueSystem(IEnumerable<Task> dependency)
        {
            List<Task> systemTasks = new();
            _accumulatedDeltaTime += MintyCore.DeltaTime;
            var enumerable = dependency as Task[] ?? dependency.ToArray();
            while (_accumulatedDeltaTime >= MintyCore.FixedDeltaTime)
            {
                systemTasks.Add(base.QueueSystem(enumerable));
                _accumulatedDeltaTime -= MintyCore.FixedDeltaTime;
            }

            return Task.WhenAll(systemTasks);
        }
    }
}