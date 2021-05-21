﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Vulkan;

namespace TechardryCoreSharp.Render
{
	struct GPUCameraData
	{
		public Matrix4x4 View;
		public Matrix4x4 Projection;
		public Matrix4x4 ViewProjection;
	}

	struct GPUSceneData
	{

	}

	class UploadContext
	{
		public VkFence Fence;
		public VkCommandPool CommandPool;
	}

	class FrameData
	{
		public VkSemaphore PresentSemaphore, RenderSemaphore;
		public VkFence Fence;

		public VkCommandPool CommandPool;
		public VkCommandBuffer MainCommandBuffer;

		public DeviceBuffer CameraBuffer;

		public VkDescriptorSet GlobalDescriptorSet;
	}

	class RenderUtils
	{

	}
}
