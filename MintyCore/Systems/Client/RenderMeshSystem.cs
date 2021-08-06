using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

	/// <summary>
	/// System to render meshes
	/// </summary>
	[ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
	[ExecutionSide(GameType.Client)]
	[ExecuteAfter(typeof(ApplyGPUCameraBufferSystem), typeof(ApplyGPUTransformBufferSystem))]
	public partial class RenderMeshSystem : ARenderSystem
	{
		[ComponentQuery]
		private ComponentQuery<object, (Renderable, Transform)> _renderableQuery = new();

		/// <inheritdoc/>
		public override void Setup()
		{
			_renderableQuery.Setup(this);

			commandLists = new (CommandList cl, bool rebuild)[_frameCount];

			for (int i = 0; i < commandLists.Length; i++)
			{
				commandLists[i] = (null, true);
			}

			EntityManager.PostEntityCreateEvent += (_, _) =>
			{
				for (int i = 0; i < commandLists.Length; i++)
				{
					commandLists[i].rebuild = true;
				}
			};
			EntityManager.PreEntityDeleteEvent += (_, _) =>
			{
				for (int i = 0; i < commandLists.Length; i++)
				{
					commandLists[i].rebuild = true;
				}
			};
		}

		(CommandList cl, bool rebuild)[] commandLists;

		/// <inheritdoc/>
		public override void PreExecuteMainThread()
		{
			if (!MintyCore.renderMode.HasFlag(MintyCore.RenderMode.Normal)) return;
			(CommandList cl, bool rebuild) = commandLists[MintyCore.Tick % _frameCount];
			if (!rebuild) return;

			cl?.FreeSecondaryCommandList();
			cl = VulkanEngine.DrawCommandList.GetSecondaryCommandList();

			cl.Begin();
			cl.SetFramebuffer(VulkanEngine.GraphicsDevice.SwapchainFramebuffer);
			commandLists[MintyCore.Tick % _frameCount] = (cl, rebuild);
		}

		/// <inheritdoc/>
		public override void PostExecuteMainThread()
		{
			if (!MintyCore.renderMode.HasFlag(MintyCore.RenderMode.Normal)) return;
			(CommandList cl, bool rebuild) = commandLists[MintyCore.Tick % _frameCount];

			if (rebuild)
				cl.End();

			VulkanEngine.DrawCommandList.ExecuteSecondaryCommandList(cl);
			rebuild = false;
			commandLists[MintyCore.Tick % _frameCount] = (cl, rebuild);
		}
		/// <inheritdoc/>
		public override void Execute()
		{
			if (!MintyCore.renderMode.HasFlag(MintyCore.RenderMode.Normal)) return;
			(CommandList cl, bool rebuild) = commandLists[MintyCore.Tick % _frameCount];
			if (!rebuild) return;

			Material? lastMaterial = null;
			Mesh? lastMesh = null;

			var entityIndexes = _entityIndexes[World];

			foreach (var entity in _renderableQuery)
			{
				Renderable renderable = entity.GetRenderable();

				var mesh = renderable.GetMesh(entity.Entity);
				var material = renderable.GetMaterialCollection();

				if (lastMaterial != material[0])
				{
					material[0].BindMaterial(cl);
					lastMaterial = material[0];
					cl.SetGraphicsResourceSet(0, _cameraBuffers[World][_frameNumber[World]].resourceSet);
					cl.SetGraphicsResourceSet(1, _transformBuffer[World].Item2);
				}

				if (mesh != lastMesh)
					mesh.BindMesh(cl);

				mesh.DrawMesh(cl, 0, (uint)entityIndexes[entity.Entity]);

				lastMesh = mesh;
			}

			commandLists[MintyCore.Tick % _frameCount] = (cl, rebuild);
		}
		/// <inheritdoc/>
		public override void Dispose()
		{

		}
		/// <inheritdoc/>
		public override Identification Identification => SystemIDs.RenderMesh;
	}
}