using System;
using System.Runtime.InteropServices;
using Ara3D;
using MintyCore.Components;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Systems.Client
{
	[ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
	[ExecutionSide(GameType.Client)]
	public class RenderMeshSystem : ASystem
	{
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
			cl = VulkanEngine.DrawCommandList.GetSecondaryCommandList();
		}

		public override void PostExecuteMainThread()
		{
			lock (MintyCore.debug)
			{
				MintyCore.debug.Add(2);
				VulkanEngine.DrawCommandList.ExecuteSecondaryCommandList(cl);
			}
			cl.FreeSecondaryCommandList();
		}
		int tick = 0;
		public override void Execute()
		{
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
			foreach (var entity in _renderableQuery)
			{
				Renderable renderable = entity.GetReadOnlyComponent<Renderable>();
				Transform transform = entity.GetReadOnlyComponent<Transform>();

				var mesh = renderable.GetMesh(entity.Entity);
				var material = renderable.GetMaterial();

				material[0].BindMaterial(cl);
				cl.SetGraphicsResourceSet(0, cameraResourceSet);

				var pushConstant = MintyCoreMod.MeshMatrixPushConstant;
				pushConstant.SetNestedValue(transform.Value);
				cl.PushConstants(pushConstant);
				
				mesh.DrawMesh(cl);

			}
			cl.End();
			tick++;
		}

		public override void Dispose()
		{

		}

		public override Identification Identification => SystemIDs.RenderMesh;
	}
}