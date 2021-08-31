using System;
using System.Runtime.InteropServices;
using MintyCore.Utils;
using Veldrid;
using Veldrid.SDL2;
using Veldrid.StartupUtilities;

namespace MintyCore.Render
{
	/// <summary>
	///     Base class to interact with the VulkanAPI through the Velrdid Library
	/// </summary>
	public static class VulkanEngine
    {
	    /// <summary>
	    ///     Delegate method for the <see cref="VulkanEngine.OnWindowResize" /> event
	    /// </summary>
	    public delegate void WindowResize(int newWidth, int newHeight);

        private const int FrameDataOverlap = 3;

        private static readonly DeletionQueue _deletionQueue = new();
        private static ImGuiRenderer? _imGuiRenderer;

        private static int _frame;

        internal static GraphicsDevice? GraphicsDevice { get; private set; }
        internal static ResourceFactory? ResourceFactory => GraphicsDevice?.ResourceFactory;

        /// <summary>
        ///     The number of swapchain used
        /// </summary>
        public static int SwapchainCount { get; private set; } = 1;

        /// <summary>
        ///     The main command list for drawing. This will be executed on the gpu every frame
        /// </summary>
        public static CommandList? DrawCommandList { get; private set; }

        /// <summary>
        ///     Event which get fired when the main window get resized
        /// </summary>
        public static event WindowResize OnWindowResize = delegate { };

        internal static void Setup()
        {
            var debug
#if DEBUG
                = true;
#else
			    = false;
#endif


            var options = new GraphicsDeviceOptions
            {
                Debug = debug,
                PreferDepthRangeZeroToOne = true,
                PreferStandardClipSpaceYDirection = true,
                HasMainSwapchain = false,
                SyncToVerticalBlank = false,
                SwapchainDepthFormat = PixelFormat.D24_UNorm_S8_UInt
            };

            GraphicsDevice = VeldridStartup.CreateVulkanGraphicsDevice(options, MintyCore.Window.GetWindow());

            if (GraphicsDevice is null)
            {
                Logger.WriteLog("Graphics device could not be initialized", LogImportance.EXCEPTION, "Rendering");
                return;
            }

            GraphicsDevice.CheckSecondaryFunctional();
            if (CommandList.SecondaryUnavailable)
                Logger.WriteLog(
                    "SecondaryCommandBuffers are not functional on this system. Switching to simulated Secondary CommandBuffers",
                    LogImportance.WARNING, "Rendering");

            //Count Swapchains on Device
            GraphicsDevice.SwapBuffers();
            while (GraphicsDevice.MainSwapchain.ImageIndex != 0)
            {
                SwapchainCount++;
                GraphicsDevice.SwapBuffers();
            }

            _imGuiRenderer = new ImGuiRenderer(GraphicsDevice,
                GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription, MintyCore.Window.GetWindow().Width,
                MintyCore.Window.GetWindow().Height);

            _deletionQueue.AddDeleteAction(() => { GraphicsDevice.Dispose(); });

            DrawCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();

            if (DrawCommandList is null)
            {
                Logger.WriteLog("DrawCommandList could not be initialized", LogImportance.EXCEPTION, "Rendering");
                return;
            }

            _deletionQueue.AddDeleteAction(() => { DrawCommandList.Dispose(); });

            MintyCore.Window.GetWindow().Resized += Resized;
        }

        private static void Resized()
        {
            var window = MintyCore.Window.GetWindow();

            GraphicsDevice?.ResizeMainWindow((uint)window.Width, (uint)window.Height);
            _imGuiRenderer?.WindowResized(window.Width, window.Height);

            OnWindowResize(window.Width, window.Height);
        }

        internal static void PrepareDraw(InputSnapshot snapshot)
        {
            _imGuiRenderer?.Update(MintyCore.DeltaTime, snapshot);

            NextFrame();

            if (DrawCommandList is null)
            {
                Logger.WriteLog("PrepareDraw is called but DrawCommandList is null", LogImportance.ERROR, "Rendering");
                return;
            }

            DrawCommandList.Begin();
            DrawCommandList.SetFramebuffer(GraphicsDevice?.SwapchainFramebuffer);

            var list = DrawCommandList.GetSecondaryCommandList();
            list.Begin();
            list.SetFramebuffer(GraphicsDevice?.SwapchainFramebuffer);
            list.ClearColorTarget(0, RgbaFloat.Blue);
            list.ClearDepthStencil(1);
            list.End();
            DrawCommandList.ExecuteSecondaryCommandList(list);
            list.FreeSecondaryCommandList();
        }

        internal static void EndDraw()
        {
            DrawCommandList?.End();
            GraphicsDevice?.SubmitCommands(DrawCommandList);

            try
            {
                GraphicsDevice?.SwapBuffers();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static void NextFrame()
        {
            _frame++;
            if (_frame >= 1000 && _frame % FrameDataOverlap == 0)
                _frame = 0;
        }

        internal static void Stop()
        {
            GraphicsDevice?.WaitForIdle();


            _deletionQueue.Flush();
        }


        #region BufferAccess

        /// <summary>
        ///     Create a Buffer on the GPU
        /// </summary>
        public static DeviceBuffer CreateBuffer(uint sizeInBytes, BufferUsage usage)
        {
            return GraphicsDevice?.ResourceFactory.CreateBuffer(new BufferDescription(sizeInBytes, usage)) ??
                   throw new MintyCoreException("Buffer could not be created");
        }

        /// <summary>
        ///     Create a Buffer on the GPU
        /// </summary>
        public static DeviceBuffer CreateBuffer<T>(BufferUsage usage, int itemCount = 1) where T : unmanaged
        {
            var desc = new BufferDescription();
            if (usage.HasFlag(BufferUsage.StructuredBufferReadWrite) ||
                usage.HasFlag(BufferUsage.StructuredBufferReadOnly))
                desc.StructureByteStride = (uint)Marshal.SizeOf<T>();
            desc.Usage = usage;
            desc.SizeInBytes = (uint)(Marshal.SizeOf<T>() * itemCount);

            return GraphicsDevice?.ResourceFactory.CreateBuffer(desc) ??
                   throw new MintyCoreException("Buffer could not be created");
        }

        /// <summary>
        ///     Update a Buffer on the GPU
        /// </summary>
        public static void UpdateBufferPtr(DeviceBuffer buffer, IntPtr data, uint size, uint bufferOffset = 0)
        {
            GraphicsDevice?.UpdateBuffer(buffer, bufferOffset, data, size);
        }

        /// <summary>
        ///     Update a Buffer on the GPU
        /// </summary>
        public static void UpdateBuffer<T>(DeviceBuffer buffer, T data, uint bufferOffset = 0) where T : unmanaged
        {
            GraphicsDevice?.UpdateBuffer(buffer, bufferOffset, data);
        }

        /// <summary>
        ///     Update a Buffer on the GPU
        /// </summary>
        public static void UpdateBuffer<T>(DeviceBuffer buffer, T[] data, uint bufferOffset = 0) where T : unmanaged
        {
            GraphicsDevice?.UpdateBuffer(buffer, bufferOffset, data);
        }

        /// <summary>
        ///     Update a Buffer on the GPU
        /// </summary>
        public static void UpdateBuffer<T>(DeviceBuffer buffer, ReadOnlySpan<T> data, uint bufferOffset = 0)
            where T : unmanaged
        {
            GraphicsDevice?.UpdateBuffer(buffer, bufferOffset, data);
        }

        #endregion
    }
}