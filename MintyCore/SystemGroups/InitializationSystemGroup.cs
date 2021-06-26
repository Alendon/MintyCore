using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.ECS;
using MintyCore.Utils;
using MintyCore.Utils.JobSystem;

namespace MintyCore.SystemGroups
{
	[RootSystemGroup]
	class InitializationSystemGroup : ASystemGroup
	{
		public override Identification Identification => SystemGroupIDs.Initialization;

	}
}
