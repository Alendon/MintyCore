using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Ara3D;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using MintyCore.Utils.JobSystem;
using Veldrid.SDL2;

namespace MintyCore
{
	public static class MintyCore
	{
		public static GameType GameType { get; private set; }
		public static Window Window { get; private set; }

		private static readonly Stopwatch _tickTimeWatch = new Stopwatch();

		public static double DeltaTime { get; private set; }
		public static int Tick { get; private set; } = 0;

		static void Main(string[] args)
		{
			Init();
			Run();
			CleanUp();
		}
		static readonly MintyCoreMod mod = new MintyCoreMod();

		private static void Init()
		{

			Logger.InitializeLog();
			JobManager.Start();
			Window = new Window();

			VulkanEngine.Setup();

			//Temporary until a proper mod loader is ready
			RegistryManager.RegistryPhase = RegistryPhase.Mods;
			ushort modID = RegistryManager.RegisterModID("techardry_core", "");
			RegistryManager.RegistryPhase = RegistryPhase.Categories;
			mod.Register(modID);
			RegistryManager.RegistryPhase = RegistryPhase.Objects;
			RegistryManager.ProcessRegistries();
			RegistryManager.RegistryPhase = RegistryPhase.None;
		}

		private static void SetDeltaTime()
		{
			_tickTimeWatch.Stop();
			DeltaTime = _tickTimeWatch.Elapsed.TotalMilliseconds;
			_tickTimeWatch.Restart();
		}

		[Flags]
		internal enum RenderMode
		{
			Normal = 1,
			Wireframe = 2,
		}

		internal static RenderMode renderMode = RenderMode.Normal;
		internal static void NextRenderMode()
		{
			var numRenderMode = (int)renderMode;
			numRenderMode++;
			numRenderMode %= 3;
			renderMode = (RenderMode)numRenderMode;
		}

		static World? world;
		private static void Run()
		{
			world = new World();

			var playerEntity = world.EntityManager.CreateEntity(ArchetypeIDs.Player, Utils.Constants.ServerID);

			Renderable renderComponent = new()
			{
				_staticMesh = 1,
				_materialCollectionId = MaterialCollectionIDs.GroundTexture,
				_staticMeshId = MeshIDs.Square
			};

			Position positionComponent = new Position();
			Rotator rotatorComponent = new Rotator();

			Random rnd = new();

			for (int x = 0; x < 100; x++)
				for (int y = 0; y < 100; y++)
				{
					var entity = world.EntityManager.CreateEntity(ArchetypeIDs.Mesh, Utils.Constants.ServerID);
					world.EntityManager.SetComponent(entity, renderComponent);

					positionComponent.Value = new Vector3(x * 2, y * 2, 0);
					world.EntityManager.SetComponent(entity, positionComponent);

					rotatorComponent.xSpeed = 0f;//(rnd.Next(200) - 100) / 100_000f;
					rotatorComponent.ySpeed = 0f;//(rnd.Next(200) - 100) / 100_000f;
					rotatorComponent.zSpeed = 0f;//(rnd.Next(200) - 100) / 100_000f;
					world.EntityManager.SetComponent(entity, rotatorComponent);
				}

			Stopwatch tick = Stopwatch.StartNew();
			Stopwatch render = new Stopwatch();
			while (Window.Exists)
			{

				if (Tick % 100 == 0)
				{
					tick.Stop();
					Logger.WriteLog($"Tick duration for the last 100 frames:", LogImportance.INFO, "General", null, true);
					Logger.WriteLog($"Complete: {tick.Elapsed.TotalMilliseconds / 100}", LogImportance.INFO, "General", null, true);
					Logger.WriteLog($"Rendering: {render.Elapsed.TotalMilliseconds / 100}", LogImportance.INFO, "General", null, true);

					render.Reset();
					tick.Reset();
					tick.Start();
				}

				SetDeltaTime();
				InputSnapshot snapshot = Window.PollEvents();

				VulkanEngine.PrepareDraw(snapshot);
				world.Tick();


				foreach (var archetypeID in ArchetypeManager.GetArchetypes().Keys)
				{
					var storage = world.EntityManager.GetArchetypeStorage(archetypeID);
					ArchetypeStorage.DirtyComponentQuery dirtyComponentQuery = new ArchetypeStorage.DirtyComponentQuery(storage);
					while (dirtyComponentQuery.MoveNext())
					{
						var current = dirtyComponentQuery.Current;
						unsafe
						{
							*(byte*)(current.ComponentPtr + ComponentManager.GetDirtyOffset(current.ComponentID)) = 0;
						}
					}
				}

				render.Start();
				VulkanEngine.EndDraw();
				render.Stop();

				Logger.AppendLogToFile();
				Tick = Tick == 1_000_000_000 ? 0 : Tick + 1;
			}
			world.Dispose();
		}

		private static void CleanUp()
		{
			JobManager.Stop();
			VulkanEngine.Stop();
			AllocationHandler.CheckUnfreed();
		}
	}
}
