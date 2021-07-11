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
	[ExecuteAfter(typeof(ApplyGPUCameraBufferSystem), typeof(ApplyGPUTransformBufferSystem))]
	[ExecutionSide(GameType.Client)]
	class RenderWireFrameSystem : ARenderSystem
	{
		public override Identification Identification => SystemIDs.RenderWireFrame;

		private ComponentQuery _renderableQuery = new();

		public override void Setup()
		{
			_renderableQuery.WithReadOnlyComponents(ComponentIDs.Renderable, ComponentIDs.Transform);
			_renderableQuery.Setup(this);
		}

		CommandList cl;

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




			cl.Begin();
			cl.SetFramebuffer(VulkanEngine.GraphicsDevice.SwapchainFramebuffer);


			Mesh? lastMesh = null;
			cl.SetPipeline(PipelineHandler.GetPipeline(PipelineIDs.WireFrame));
			cl.SetGraphicsResourceSet(0, _cameraBuffers[World][_frameNumber[World]].resourceSet);

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

		}
	}
}
