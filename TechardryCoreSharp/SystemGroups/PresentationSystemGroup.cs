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
	[ExecuteAfter(typeof(FinalizationSystemGroup))]
	class PresentationSystemGroup : ASystemGroup
	{
		public override Identification Identification => SystemGroupIDs.Presentation;

		public override void PreExecuteMainThread()
		{
			base.PreExecuteMainThread();
			TechardryCore.VulkanEngine.PrepareDraw();
		}

		public override void PostExecuteMainThread()
		{
			base.PostExecuteMainThread();
			TechardryCore.VulkanEngine.EndDraw();
		}
	}
}
