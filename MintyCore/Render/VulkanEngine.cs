﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using Ara3D;
using MintyCore.Components;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;
using Vulkan;

namespace MintyCore.Render
{
    public static class VulkanEngine
    {
        private const int FrameDataOverlap = 3;

        internal static GraphicsDevice GraphicsDevice { get; private set; }
        internal static ResourceFactory ResourceFactory => GraphicsDevice.ResourceFactory;

        private static readonly DeletionQueue _deletionQueue = new DeletionQueue();
        public static CommandList DrawCommandList { get; private set; }

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
                SyncToVerticalBlank = true,
                SwapchainDepthFormat = PixelFormat.D24_UNorm_S8_UInt,
            };

            GraphicsDevice = VeldridStartup.CreateVulkanGraphicsDevice(options, MintyCore.Window.GetWindow());

            _deletionQueue.AddDeleteAction(() => { GraphicsDevice.Dispose(); });

            DrawCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();
            _deletionQueue.AddDeleteAction(() => { DrawCommandList.Dispose(); });

			MintyCore.Window.GetWindow().Resized += Resized;
        }

		private static void Resized()
		{
            var window = MintyCore.Window.GetWindow();

            GraphicsDevice.ResizeMainWindow((uint)window.Width, (uint)window.Height);

        }

		public static void PrepareDraw()
        {
            lock (MintyCore.debug)
            {
                MintyCore.debug.Add(0);

                

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
        }

        public static void EndDraw()
        {
            lock (MintyCore.debug)
            {
                MintyCore.debug.Add(3);
                DrawCommandList.End();
                GraphicsDevice.SubmitCommands(DrawCommandList);
                GraphicsDevice.SwapBuffers();
            }
        }

        private static void NextFrame()
        {
            _frame++;
            if (_frame >= 1000 && _frame % FrameDataOverlap == 0)
                _frame = 0;
        }


        #region BufferAccess

        public static DeviceBuffer CreateBuffer(uint sizeInBytes, BufferUsage usage)
        {
            return GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(sizeInBytes, usage));
        }

        public static void UpdateBuffer(DeviceBuffer buffer, IntPtr data, uint size, uint bufferOffset = 0)
        {
            GraphicsDevice.UpdateBuffer(buffer, bufferOffset, data, size);
        }

        public static void UpdateBuffer<T>(DeviceBuffer buffer, T data, uint bufferOffset = 0) where T : unmanaged
        {
            GraphicsDevice.UpdateBuffer(buffer, bufferOffset, data);
        }

        public static void UpdateBuffer<T>(DeviceBuffer buffer, T[] data, uint bufferOffset = 0) where T : unmanaged
        {
            GraphicsDevice.UpdateBuffer(buffer, bufferOffset, data);
        }

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