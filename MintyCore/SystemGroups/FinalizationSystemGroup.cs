using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.SystemGroups
{
	[RootSystemGroup]
	[ExecuteAfter(typeof(SimulationSystemGroup))]
	class FinalizationSystemGroup : ASystemGroup
	{
		public override Identification Identification => SystemGroupIDs.Finalization;
	}
}
