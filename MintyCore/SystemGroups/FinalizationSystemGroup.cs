using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.SystemGroups
{
    [RootSystemGroup]
    [ExecuteAfter(typeof(SimulationSystemGroup))]
    internal class FinalizationSystemGroup : ASystemGroup
    {
        public override Identification Identification => SystemGroupIDs.Finalization;
    }
}