using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.ECS;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.SystemGroups
{
	[RootSystemGroup]
	[ExecuteAfter(typeof(InitializationSystemGroup))]
	class SimulationSystemGroup : ASystemGroup
	{
		public override Identification Identification => SystemGroupIDs.Simulation;
	}
}
