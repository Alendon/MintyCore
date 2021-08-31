using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.SystemGroups
{
    [RootSystemGroup]
    [ExecuteAfter(typeof(InitializationSystemGroup))]
    internal class SimulationSystemGroup : ASystemGroup
    {
        public override Identification Identification => SystemGroupIDs.Simulation;
    }
}