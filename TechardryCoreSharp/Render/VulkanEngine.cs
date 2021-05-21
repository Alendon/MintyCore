using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Utils;
using Veldrid;
using Veldrid.StartupUtilities;
using Vulkan;

namespace TechardryCoreSharp.Render
{
	public class VulkanEngine : IDisposable
	{
		private const int FrameOverlap = 2;

		internal GraphicsDevice _graphicsDevice { get; private set; }
		private BackendInfoVulkan _vulkanInfo;

		private DeletionQueue _deletionQueue = new DeletionQueue();

		internal VulkanEngine() { }

		internal void Setup()
		{

			bool debug;
#if DEBUG
			debug = true;
#else
			debug = false;
#endif
			GraphicsDeviceOptions options = new GraphicsDeviceOptions( debug, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, true, ResourceBindingModel.Improved, true, true, true );
			_graphicsDevice = VeldridStartup.CreateVulkanGraphicsDevice( options, TechardryCore.Window.GetWindow() );

			_deletionQueue.AddDeleteAction( () =>
			{
				_graphicsDevice.Dispose();
			} );
		}

		public void Dispose()
		{
			_deletionQueue.Flush();
		}

	}


}
