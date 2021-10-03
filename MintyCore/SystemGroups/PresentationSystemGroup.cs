using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.SystemGroups
{
    [ExecutionSide(GameType.CLIENT)]
    [RootSystemGroup]
    [ExecuteAfter(typeof(FinalizationSystemGroup))]
    public class PresentationSystemGroup : ASystemGroup
    {
        public override Identification Identification => SystemGroupIDs.Presentation;
    }
}