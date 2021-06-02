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
	[ExecuteAfter(typeof(FinalizationSystemGroup))]
	class PresentationSystemGroup : ASystemGroup
	{
		public override Identification Identification => SystemGroupIDs.Presentation;

		public override void PreExecuteMainThread()
		{
			base.PreExecuteMainThread();
			MintyCore.VulkanEngine.PrepareDraw();

			var commandList = MintyCore.VulkanEngine.DrawCommandList.GetSecondaryCommandList();
			commandList.Begin();
			commandList.SetFramebuffer( MintyCore.VulkanEngine.GraphicsDevice.SwapchainFramebuffer );
			commandList.ClearColorTarget( 0, Veldrid.RgbaFloat.Orange );
			commandList.End();
			MintyCore.VulkanEngine.DrawCommandList.ExecuteSecondaryCommandList( commandList );
			commandList.FreeSecondaryCommandList();
		}

		public override void PostExecuteMainThread()
		{
			base.PostExecuteMainThread();
			MintyCore.VulkanEngine.EndDraw();
		}
	}
}
