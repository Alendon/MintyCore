using Ara3D;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace MintyCore.Systems.Client
{
	[ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
	[ExecuteAfter(typeof(IncreaseFrameNumberSystem))]
	class ApplyGPUCameraBufferSystem : ARenderSystem
	{
		public override Identification Identification => SystemIDs.ApplyGPUCameraBuffer;

		private ComponentQuery _cameraQuery = new();

		public override void Dispose()
		{
			foreach (var item in _cameraBuffers[World])
			{
				item.buffer.Dispose();
				item.resourceSet.Dispose();
			}
			_cameraBuffers.Remove(World);
		}

		public override void Execute()
		{
			foreach (var entity in _cameraQuery)
			{
				Camera camera = entity.GetReadOnlyComponent<Camera>();
				Position position = entity.GetReadOnlyComponent<Position>();


				var cameraMatrix = Matrix4x4.CreateLookAt(position.Value, position.Value + new Vector3(0, 0, -1), new Vector3(0, 1, 0));
				var camProjection = Matrix4x4.CreatePerspectiveFieldOfView(camera.Fov, MintyCore.Window.GetWindow().Width / MintyCore.Window.GetWindow().Height, 0.1f, 200f);
				VulkanEngine.UpdateBuffer( _cameraBuffers[World][_frameNumber[World]].buffer , cameraMatrix * camProjection);
			}
		}

		public override void Setup()
		{
			_cameraQuery.WithReadOnlyComponents(ComponentIDs.Camera, ComponentIDs.Position);
			_cameraQuery.Setup(this);

			_cameraBuffers.Add(World, new (DeviceBuffer, ResourceSet)[_frameCount]);
			for (int i = 0; i < _cameraBuffers[World].Length; i++)
			{
				var buffer = VulkanEngine.CreateBuffer<Matrix4x4>(BufferUsage.UniformBuffer);
				ResourceSetDescription resourceSetDescription = new(MintyCoreMod.CameraResourceLayout, buffer);
				var resourceSet = VulkanEngine.ResourceFactory.CreateResourceSet(ref resourceSetDescription);

				_cameraBuffers[World][i].buffer = buffer;
				_cameraBuffers[World][i].resourceSet = resourceSet;
			}
		}
	}
}
