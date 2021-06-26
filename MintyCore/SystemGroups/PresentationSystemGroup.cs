using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.ECS;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.SystemGroups
{
	[ExecutionSide(GameType.Client)]
	[RootSystemGroup]
	[ExecuteAfter(typeof(FinalizationSystemGroup))]
	class PresentationSystemGroup : ASystemGroup
	{
		public override Identification Identification => SystemGroupIDs.Presentation;
	}
}
