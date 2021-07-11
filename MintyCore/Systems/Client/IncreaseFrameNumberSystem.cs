using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Systems.Client
{

	[ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
	class IncreaseFrameNumberSystem : ARenderSystem
	{
		public override Identification Identification => SystemIDs.IncreaseFrameNumber;

		public override void Dispose()
		{
			_frameNumber.Remove(World);
		}

		public override void Execute()
		{
			_frameNumber[World]++;
			_frameNumber[World] %= _frameCount;
		}

		public override void Setup()
		{
			_frameNumber.Add(World, 0);
		}
	}
}
