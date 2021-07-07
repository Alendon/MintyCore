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

		private static Stopwatch _tickTimeWatch = new Stopwatch();

		public static double DeltaTime { get; private set; }
		public static int Tick { get; private set; } = 0;

		static void Main(string[] args)
		{
			Init();
			Run();
			CleanUp();
		}
		static MintyCoreMod mod = new MintyCoreMod();

		private static void Init()
		{
			

			JobManager.Start();
			Window = new Window();

			VulkanEngine.Setup();

			//Temporary until a proper mod loader is ready
			RegistryManager.RegistryPhase = true;
			mod.Register(RegistryManager.RegisterModID("techardry_core", ""));
			RegistryManager.ProcessRegistries();
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

			Renderable renderComponent = new Renderable();
			renderComponent._staticMesh = 1;
			renderComponent._materialCollectionId = MaterialCollectionIDs.BasicColorCollection;
			renderComponent._staticMeshId = MeshIDs.Suzanne;

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

					rotatorComponent.xSpeed = rnd.Next(100) / 100_000f;
					rotatorComponent.ySpeed = rnd.Next(100) / 100_000f;
					rotatorComponent.zSpeed = rnd.Next(100) / 100_000f;
					world.EntityManager.SetComponent(entity, rotatorComponent);
				}

			Stopwatch sw = Stopwatch.StartNew();
			while (Window.Exists)
			{

				if (Tick % 100 == 0)
				{
					sw.Stop();
					Console.WriteLine(sw.Elapsed.TotalMilliseconds / 100);
					sw.Reset();
					sw.Start();
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

				VulkanEngine.EndDraw();


				Tick = Tick == 1_000_000_000 ? 0 : Tick + 1;
			}
		}

		private static void CleanUp()
		{
			JobManager.Stop();
			VulkanEngine.Stop();
			AllocationHandler.CheckUnfreed();
		}
	}
}
