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
	partial class RenderWireFrameSystem : ARenderSystem
	{
		public override Identification Identification => SystemIDs.RenderWireFrame;

		[ComponentQuery]
		private ComponentQuery<object, (Renderable,Transform)> _renderableQuery = new();

		public override void Setup()
		{
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
			cl.SetGraphicsResourceSet(1, _transformBuffer[World].Item2);

			var entityIndexes = _entityIndexes[World];

			foreach (var entity in _renderableQuery)
			{
				Renderable renderable = entity.GetRenderable();

				var mesh = renderable.GetMesh(entity.Entity);

				if (mesh != lastMesh)
					mesh.BindMesh(cl);

				mesh.DrawMesh(cl, 0, (uint)entityIndexes[entity.Entity]);
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
