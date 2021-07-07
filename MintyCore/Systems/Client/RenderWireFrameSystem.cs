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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace MintyCore.Systems.Client
{
	[ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
	[ExecuteAfter(typeof(RenderMeshSystem))]
	[ExecutionSide(GameType.Client)]
	class RenderWireFrameSystem : ASystem
	{
		public override Identification Identification => SystemIDs.RenderWireFrame;

		private ComponentQuery _renderableQuery = new();
		private ComponentQuery _cameraQuery = new();

		public override void Setup()
		{
			_renderableQuery.WithReadOnlyComponents(ComponentIDs.Renderable, ComponentIDs.Transform);
			_renderableQuery.Setup(this);

			_cameraQuery.WithReadOnlyComponents(ComponentIDs.Camera, ComponentIDs.Position);
			_cameraQuery.Setup(this);


			cameraBuffer = VulkanEngine.CreateBuffer((uint)Marshal.SizeOf<Matrix4x4>(), BufferUsage.UniformBuffer);


			ResourceSetDescription cameraSetDescription = new(MintyCoreMod.CameraResourceLayout, cameraBuffer);
			cameraResourceSet = VulkanEngine.GraphicsDevice.ResourceFactory.CreateResourceSet(cameraSetDescription);
		}

		CommandList cl;
		ResourceSet cameraResourceSet;
		DeviceBuffer cameraBuffer;


		public override void PreExecuteMainThread()
		{

		}

		public override void PostExecuteMainThread()
		{

		}

		public override void Execute()
		{
			if (!MintyCore.renderMode.HasFlag(MintyCore.RenderMode.Wireframe)) return;

			cl = VulkanEngine.DrawCommandList.GetSecondaryCommandList();

			foreach (var entity in _cameraQuery)
			{
				Camera camera = entity.GetReadOnlyComponent<Camera>();
				Position position = entity.GetReadOnlyComponent<Position>();


				var cameraMatrix = Matrix4x4.CreateLookAt(position.Value, position.Value + new Vector3(0, 0, -1), new Vector3(0, 1, 0));
				var camProjection = Matrix4x4.CreatePerspectiveFieldOfView(camera.Fov, MintyCore.Window.GetWindow().Width / MintyCore.Window.GetWindow().Height, 0.1f, 200f);
				VulkanEngine.UpdateBuffer(cameraBuffer, cameraMatrix * camProjection);
			}



			cl.Begin();
			cl.SetFramebuffer(VulkanEngine.GraphicsDevice.SwapchainFramebuffer);


			Mesh? lastMesh = null;
			cl.SetPipeline(PipelineHandler.GetPipeline(PipelineIDs.WireFrame));
			cl.SetGraphicsResourceSet(0, cameraResourceSet);

			foreach (var entity in _renderableQuery)
			{
				Renderable renderable = entity.GetReadOnlyComponent<Renderable>();
				Transform transform = entity.GetReadOnlyComponent<Transform>();

				var mesh = renderable.GetMesh(entity.Entity);

				var pushConstant = MintyCoreMod.MeshMatrixPushConstant;
				pushConstant.SetNestedValue(transform.Value);
				cl.PushConstants(pushConstant);

				if (mesh != lastMesh)
					mesh.BindMesh(cl);

				mesh.DrawMesh(cl);
				lastMesh = mesh;

			}
			cl.End();


			VulkanEngine.DrawCommandList.ExecuteSecondaryCommandList(cl);

			cl.FreeSecondaryCommandList();
		}

		public override void Dispose()
		{
			cameraResourceSet.Dispose();
			cameraBuffer.Dispose();
		}
	}
}
