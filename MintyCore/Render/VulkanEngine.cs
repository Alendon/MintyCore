using System;
using MintyCore.Utils;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;
using Vulkan;

namespace MintyCore.Render
{
    public class VulkanEngine
    {
        internal GraphicsDevice GraphicsDevice { get; private set; }

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
            GraphicsDevice = VeldridStartup.CreateVulkanGraphicsDevice(options, MintyCore.Window.GetWindow());

            _deletionQueue.AddDeleteAction(() => { GraphicsDevice.Dispose(); });

            DrawCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();
            _deletionQueue.AddDeleteAction(() => { DrawCommandList.Dispose(); });
        }

        public void PrepareDraw()
        {
            DrawCommandList.Begin();
            DrawCommandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
        }

        public void EndDraw()
        {
            DrawCommandList.End();
            GraphicsDevice.SubmitCommands(DrawCommandList);
            GraphicsDevice.SwapBuffers();
        }

        public DeviceBuffer CreateBuffer(uint sizeInBytes, BufferUsage usage)
        {
            return GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(sizeInBytes, usage));
        }

        public void UpdateBuffer(DeviceBuffer buffer, IntPtr data, uint size, uint bufferOffset = 0)
        {
            GraphicsDevice.UpdateBuffer(buffer, bufferOffset, data, size);
        }

        public void UpdateBuffer<T>(DeviceBuffer buffer, T data, uint bufferOffset = 0) where T : unmanaged
        {
            GraphicsDevice.UpdateBuffer(buffer, bufferOffset, data);
        }

        public void UpdateBuffer<T>(DeviceBuffer buffer, T[] data, uint bufferOffset = 0) where T : unmanaged
        {
            GraphicsDevice.UpdateBuffer(buffer, bufferOffset, data);
        }
        
        public void UpdateBuffer<T>(DeviceBuffer buffer, ReadOnlySpan<T> data, uint bufferOffset = 0) where T : unmanaged
        {
            GraphicsDevice.UpdateBuffer(buffer, bufferOffset, data);
        }

        ~VulkanEngine()
        {
            GraphicsDevice.WaitForIdle();
            _deletionQueue.Flush();
        }
    }
}