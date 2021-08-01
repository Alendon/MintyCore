using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using Ara3D;
using ImGuiNET;
using MintyCore.Components;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using Veldrid;
using Veldrid.SDL2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;
using Vulkan;

namespace MintyCore.Render
{
	/// <summary>
	/// Base class to interact with the VulkanAPI through the Velrdid Library
	/// </summary>
	public static class VulkanEngine
	{
		private const int FrameDataOverlap = 3;

		internal static GraphicsDevice GraphicsDevice { get; private set; }
		internal static ResourceFactory ResourceFactory => GraphicsDevice.ResourceFactory;

		private static readonly DeletionQueue _deletionQueue = new DeletionQueue();

		/// <summary>
		/// The main command list for drawing. This will be executed on the gpu every frame
		/// </summary>
		public static CommandList DrawCommandList { get; private set; }
		private static ImGuiRenderer ImGuiRenderer;

		private static int _frame = 0;

		internal static void Setup()
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
				SyncToVerticalBlank = false,
				SwapchainDepthFormat = PixelFormat.D24_UNorm_S8_UInt,
			};

			GraphicsDevice = VeldridStartup.CreateVulkanGraphicsDevice(options, MintyCore.Window.GetWindow());
			GraphicsDevice.CheckSecondaryFunctional();
			if (CommandList.SecondaryUnavailable)
			{
				Logger.WriteLog("SecondaryCommandBuffers are not functional on this system. Switching to simulated Secondary CommandBuffers", LogImportance.WARNING, "Rendering");
			}

			ImGuiRenderer = new ImGuiRenderer(GraphicsDevice, GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription, MintyCore.Window.GetWindow().Width, MintyCore.Window.GetWindow().Height);

			_deletionQueue.AddDeleteAction(() => { GraphicsDevice.Dispose(); });

			DrawCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();
			_deletionQueue.AddDeleteAction(() => { DrawCommandList.Dispose(); });

			MintyCore.Window.GetWindow().Resized += Resized;
		}

		private static void Resized()
		{
			var window = MintyCore.Window.GetWindow();

			GraphicsDevice.ResizeMainWindow((uint)window.Width, (uint)window.Height);
			ImGuiRenderer.WindowResized(window.Width, window.Height);
		}

		internal static void PrepareDraw(InputSnapshot snaphot)
		{
			ImGuiRenderer.Update((float)MintyCore.DeltaTime, snaphot);

			NextFrame();

			DrawCommandList.Begin();
			DrawCommandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);

			var list = DrawCommandList.GetSecondaryCommandList();
			list.Begin();
			list.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
			list.ClearColorTarget(0, RgbaFloat.Blue);
			list.ClearDepthStencil(1);
			list.End();
			DrawCommandList.ExecuteSecondaryCommandList(list);
			list.FreeSecondaryCommandList();

		}

		internal static void EndDraw()
		{
			DrawCommandList.End();
			GraphicsDevice.SubmitCommands(DrawCommandList);

			try
			{
				GraphicsDevice.SwapBuffers();
			}
			catch (Exception) { }

		}

		private static void NextFrame()
		{
			_frame++;
			if (_frame >= 1000 && _frame % FrameDataOverlap == 0)
				_frame = 0;
		}


		#region BufferAccess

		/// <summary>
		/// Create a Buffer on the GPU
		/// </summary>
		public static DeviceBuffer CreateBuffer(uint sizeInBytes, BufferUsage usage)
		{
			return GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(sizeInBytes, usage));
		}

		/// <summary>
		/// Create a Buffer on the GPU
		/// </summary>
		public static DeviceBuffer CreateBuffer<T>(BufferUsage usage, int itemCount = 1) where T : unmanaged
		{
			BufferDescription desc = new BufferDescription();
			if (usage.HasFlag(BufferUsage.StructuredBufferReadWrite) || usage.HasFlag(BufferUsage.StructuredBufferReadOnly))
			{
				desc.StructureByteStride = (uint)Marshal.SizeOf<T>();
			}
			desc.Usage = usage;
			desc.SizeInBytes = (uint)(Marshal.SizeOf<T>() * itemCount);

			return GraphicsDevice.ResourceFactory.CreateBuffer(desc);
		}

		/// <summary>
		/// Update a Buffer on the GPU
		/// </summary>
		public static void UpdateBufferPtr(DeviceBuffer buffer, IntPtr data, uint size, uint bufferOffset = 0)
		{
			GraphicsDevice.UpdateBuffer(buffer, bufferOffset, data, size);
		}

		/// <summary>
		/// Update a Buffer on the GPU
		/// </summary>
		public static void UpdateBuffer<T>(DeviceBuffer buffer, T data, uint bufferOffset = 0) where T : unmanaged
		{
			GraphicsDevice.UpdateBuffer(buffer, bufferOffset, data);
		}

		/// <summary>
		/// Update a Buffer on the GPU
		/// </summary>
		public static void UpdateBuffer<T>(DeviceBuffer buffer, T[] data, uint bufferOffset = 0) where T : unmanaged
		{
			GraphicsDevice.UpdateBuffer(buffer, bufferOffset, data);
		}

		/// <summary>
		/// Update a Buffer on the GPU
		/// </summary>
		public static void UpdateBuffer<T>(DeviceBuffer buffer, ReadOnlySpan<T> data, uint bufferOffset = 0)
			where T : unmanaged
		{
			GraphicsDevice.UpdateBuffer(buffer, bufferOffset, data);
		}

		#endregion

		internal static void Stop()
		{
			GraphicsDevice.WaitForIdle();


			_deletionQueue.Flush();
		}



	}
}