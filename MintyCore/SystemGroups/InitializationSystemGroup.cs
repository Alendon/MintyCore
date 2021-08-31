using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.SystemGroups
{
    [RootSystemGroup]
    internal class InitializationSystemGroup : ASystemGroup
    {
        public override Identification Identification => SystemGroupIDs.Initialization;
    }
}