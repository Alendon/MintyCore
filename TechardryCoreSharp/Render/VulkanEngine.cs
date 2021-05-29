using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Utils;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;
using Vulkan;

namespace TechardryCoreSharp.Render
{
	public class VulkanEngine
	{


		internal GraphicsDevice GraphicsDevice { get; private set; }
		private BackendInfoVulkan _vulkanInfo;
		private VkCommandPool _commandPool;


		private readonly DeletionQueue _deletionQueue = new DeletionQueue();

		public CommandList DrawCommandList { get; private set; }

		internal VulkanEngine()
		{
		}

		internal void Setup()
		{
			bool debug
#if DEBUG
			= true;
#else
			= false;
#endif

			GraphicsDeviceOptions options = new GraphicsDeviceOptions()
			{
				Debug = debug,
				PreferDepthRangeZeroToOne = true,
				PreferStandardClipSpaceYDirection = true,
				HasMainSwapchain = false,
				SyncToVerticalBlank = true,
				SwapchainDepthFormat = PixelFormat.D24_UNorm_S8_UInt
			};
			GraphicsDevice = VeldridStartup.CreateVulkanGraphicsDevice( options, TechardryCore.Window.GetWindow() );
			_vulkanInfo = GraphicsDevice.GetVulkanInfo();

			_deletionQueue.AddDeleteAction( () =>
			{
				GraphicsDevice.Dispose();
			} );

			DrawCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();
			_deletionQueue.AddDeleteAction( () =>
			{
				DrawCommandList.Dispose();
			} );
		}

		private void SetupCommandBuffers()
		{
			
		}

		public void PrepareDraw()
		{
			DrawCommandList.Begin();
			DrawCommandList.SetFramebuffer( GraphicsDevice.SwapchainFramebuffer );
			DrawCommandList.ClearColorTarget( 0, RgbaFloat.Cyan );
		}

		public void EndDraw()
		{
			DrawCommandList.End();
			GraphicsDevice.SubmitCommands( DrawCommandList );
			GraphicsDevice.SwapBuffers();
		}

		~VulkanEngine()
		{
			GraphicsDevice.WaitForIdle();
			_deletionQueue.Flush();
		}

	}


}
