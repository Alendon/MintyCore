using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Ara3D;
using MintyCore.Components.Client;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using MintyCore.Utils.JobSystem;

namespace MintyCore
{
	public static class MintyCore
	{
		public static GameType GameType { get; private set; }
		public static Window Window { get; private set; }
		
		private static Stopwatch _tickTimeWatch = new Stopwatch();

		public static List<int> debug = new();

		public static double DeltaTime { get; private set; }

		static void Main( string[] args )
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
			mod.Register( RegistryManager.RegisterModID( "techardry_core", "" ) );
			RegistryManager.ProcessRegistries();
		}

		private static void SetDeltaTime()
		{
			_tickTimeWatch.Stop();
			DeltaTime = _tickTimeWatch.Elapsed.TotalMilliseconds;
			_tickTimeWatch.Restart();
		}


		private static void Run()
		{
			World world = new ();

			var playerEntity = world.EntityManager.CreateEntity(ArchetypeIDs.Player, Utils.Constants.ServerID);

			var meshEntity = world.EntityManager.CreateEntity(ArchetypeIDs.Mesh, Utils.Constants.ServerID);
			ref Renderable renderable = ref
				world.EntityManager.GetRefComponent<Renderable>(meshEntity, ComponentIDs.Renderable);

			DefaultVertex[] defaultVertices = new[]
			{
				new DefaultVertex(new Vector3(-1, -0.5f, 0), new Vector3(1f, 0, 0), Vector3.Zero, Vector2.Zero),
				new DefaultVertex(new Vector3(0, 1f, 0), new Vector3(0, 0, 1f), Vector3.Zero, Vector2.Zero),
				new DefaultVertex(new Vector3(1, -0.5f, 0), new Vector3(0, 1f, 0), Vector3.Zero, Vector2.Zero),
			
			};

			var dynamicMesh = MeshHandler.CreateDynamicMesh(defaultVertices, meshEntity);
			renderable._staticMesh = 1;
			renderable._dynamicMeshId = dynamicMesh.id;
			renderable._staticMeshId = MeshIDs.Suzanne;
			renderable._materialCollectionId = MaterialCollectionIDs.BasicColorCollection;
			
			while ( Window.Exists )
			{
				SetDeltaTime();

				VulkanEngine.PrepareDraw();
				world.Tick();
				VulkanEngine.EndDraw();


				Window.PollEvents();
				
			}
		}

		private static void CleanUp()
		{
			JobManager.Stop();
			VulkanEngine.Stop();
		}
	}
}
