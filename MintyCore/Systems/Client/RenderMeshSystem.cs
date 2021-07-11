using System;
using System.Collections.Generic;
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
	[ExecuteAfter(typeof(ApplyGPUCameraBufferSystem), typeof(ApplyGPUTransformBufferSystem))]
	public class RenderMeshSystem : ARenderSystem
	{
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
			if (!MintyCore.renderMode.HasFlag(MintyCore.RenderMode.Normal)) return;
			cl = VulkanEngine.DrawCommandList.GetSecondaryCommandList();

			cl.Begin();
			cl.SetFramebuffer(VulkanEngine.GraphicsDevice.SwapchainFramebuffer);

			Material? lastMaterial = null;
			Mesh? lastMesh = null;

			var entityIndexes = _entityIndexes[World];
			
			foreach (var entity in _renderableQuery)
			{
				Renderable renderable = entity.GetReadOnlyComponent<Renderable>();
				Transform transform = entity.GetReadOnlyComponent<Transform>();

				var mesh = renderable.GetMesh(entity.Entity);
				var material = renderable.GetMaterial();

				if (lastMaterial != material[0])
				{
					material[0].BindMaterial(cl);
					lastMaterial = material[0];
					cl.SetGraphicsResourceSet(0, _cameraBuffers[World][_frameNumber[World]].resourceSet);
					cl.SetGraphicsResourceSet(1, _transformBuffer[World].Item2);
				}

				if (mesh != lastMesh)
					mesh.BindMesh(cl);

				mesh.DrawMesh(cl,0, (uint)entityIndexes[entity.Entity]);

				lastMesh = mesh;
			}

			cl.End();


			VulkanEngine.DrawCommandList.ExecuteSecondaryCommandList(cl);

			cl.FreeSecondaryCommandList();
		}

		public override void Dispose()
		{

		}

		public override Identification Identification => SystemIDs.RenderMesh;
	}
}