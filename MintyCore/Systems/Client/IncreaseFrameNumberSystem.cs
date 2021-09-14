using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Client
{
    [ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
    internal class IncreaseFrameNumberSystem : ARenderSystem
    {
        public override Identification Identification => SystemIDs.IncreaseFrameNumber;

        public override void Dispose()
        {
            if (World is null) return;
            FrameNumber.Remove(World);
        }

        protected override void Execute()
        {
            if (World is null) return;
            FrameNumber[World]++;
            FrameNumber[World] %= FrameCount;
        }

        public override void Setup()
        {
            if (World is null) return;
            FrameNumber.Add(World, 0);
        }
    }
}