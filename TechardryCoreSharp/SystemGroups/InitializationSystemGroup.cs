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
	class InitializationSystemGroup : ASystemGroup
	{
		public override Identification Identification => SystemGroupIDs.Initialization;
	}
}
