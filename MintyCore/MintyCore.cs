using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic.Collisions;
using MintyCore.Components.Common.Physic.Dynamics;
using MintyCore.Components.Common.Physic.Forces;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Physics;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using MintyCore.Utils.UnmanagedContainers;

using Veldrid.SDL2;

namespace MintyCore
{
	/// <summary>
	/// Engine/CoreGame main class
	/// </summary>
	public static class MintyCore
	{
		/// <summary>
		/// The <see cref="GameType"/> of the running instance
		/// </summary>
		public static GameType GameType { get; private set; }

		/// <summary>
		/// The reference to the main <see cref="Window"/>
		/// </summary>
		public static Window Window { get; private set; }

		private static readonly Stopwatch _tickTimeWatch = new Stopwatch();

		/// <summary>
		/// The delta time of the current tick as double in Seconds
		/// </summary>
		public static double DDeltaTime { get; private set; }

		/// <summary>
		/// The delta time of the current tick in Seconds
		/// </summary>
		public static float DeltaTime { get; private set; }

		/// <summary>
		/// Fixed delta time for physics simulation in Seconds
		/// </summary>
		public static float FixedDeltaTime { get; private set; } = 0.02f;


		/// <summary>
		/// The current Tick number. Capped between 0 and 1_000_000_000 (exclusive)
		/// </summary>
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
			DDeltaTime = _tickTimeWatch.Elapsed.TotalSeconds;
			DeltaTime = (float)_tickTimeWatch.Elapsed.TotalSeconds;
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
			numRenderMode %= 3;
			numRenderMode++;
			renderMode = (RenderMode)numRenderMode;
		}

		static World? world;
		private static void Run()
		{
			world = new World();

			var playerEntity = world.EntityManager.CreateEntity(ArchetypeIDs.Player, Utils.Constants.ServerID);

			Position playerPos = new Position() { Value = new Vector3(0, 0, 5) };
			world.EntityManager.SetComponent(playerEntity, playerPos);
			//SpawnWallOfDirt();

			var physicsCube = world.EntityManager.CreateEntity(ArchetypeIDs.RigidBody);

			Renderable renderable = new();
			renderable.SetMesh(MeshIDs.Cube);
			renderable._materialCollectionId = MaterialCollectionIDs.BasicColorCollection;

			Position cubePos = new Position() { Value = new Vector3(2, 5, 0) };

			Mass mass = new Mass();
			mass.MassValue = 1;

			Rotation rotation = new();
			rotation.Value = Quaternion.CreateFromYawPitchRoll(0, 0, 45);

			Inertia inertia = new Inertia();
			inertia.InertiaTensor = InertiaTensorCalculator.Cuboid(Vector3.One, 1);

			UnmanagedArray<Vector3> vertices = new(8);
			vertices[0] = new( 0.5f,  0.5f, -0.5f);
			vertices[1] = new( 0.5f, -0.5f, -0.5f);
			vertices[2] = new( 0.5f,  0.5f,  0.5f);
			vertices[3] = new( 0.5f, -0.5f,  0.5f);
			vertices[4] = new(-0.5f,  0.5f, -0.5f);
			vertices[5] = new(-0.5f, -0.5f, -0.5f);
			vertices[6] = new(-0.5f,  0.5f,  0.5f);
			vertices[7] = new(-0.5f, -0.5f,  0.5f);
			
			UnmanagedArray<ColliderContainer> colliders = new(1);
			colliders[0] = new() { Vertices = vertices, Transform = Matrix4x4.Identity};

			Collider collider = new();
			collider.SetColliders(colliders);

			world.EntityManager.SetComponent(physicsCube, renderable);
			world.EntityManager.SetComponent(physicsCube, mass);
			world.EntityManager.SetComponent(physicsCube, inertia);
			world.EntityManager.SetComponent(physicsCube, cubePos);
			world.EntityManager.SetComponent(physicsCube, collider);
			world.EntityManager.SetComponent(physicsCube, rotation);



			var plane = world.EntityManager.CreateEntity(ArchetypeIDs.RigidBody);
			mass.SetInfiniteMass();
			var inverseInertia = inertia.InverseInertiaTensor;
			inverseInertia.M11 = 0;
			inverseInertia.M22 = 0;
			inverseInertia.M33 = 0;
			inertia.InverseInertiaTensor = inverseInertia;

			var scale = new Scale();
			scale.Value = new(3, 1, 3);
			cubePos.Value = new(0, -5, 0);

			world.EntityManager.SetComponent(plane, renderable);
			world.EntityManager.SetComponent(plane, mass);
			world.EntityManager.SetComponent(plane, inertia);
			world.EntityManager.SetComponent(plane, scale);
			world.EntityManager.SetComponent(plane, cubePos);
			world.EntityManager.SetComponent(plane, collider);

			collider.DecreaseRefCount();

			world.SetupTick();

			Stopwatch tick = Stopwatch.StartNew();
			Stopwatch render = new Stopwatch();
			while (Window.Exists)
			{

				if (Tick % 100 == 0)
				{
					tick.Stop();
					//Logger.WriteLog($"Tick duration for the last 100 frames:", LogImportance.INFO, "General", null, true);
					//Logger.WriteLog($"Complete: {tick.Elapsed.TotalMilliseconds / 100}", LogImportance.INFO, "General", null, true);
					//Logger.WriteLog($"Rendering: {render.Elapsed.TotalMilliseconds / 100}", LogImportance.INFO, "General", null, true);

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
					ArchetypeStorage.DirtyComponentQuery dirtyComponentQuery = new(storage);
					while (dirtyComponentQuery.MoveNext())
					{

					}
				}

				render.Start();
				VulkanEngine.EndDraw();
				render.Stop();

				Logger.AppendLogToFile();
				Tick = (Tick + 1) % 1_000_000_000;
			}
			world.Dispose();
			
			vertices.DecreaseRefCount();
		}

		private static void SpawnWallOfDirt()
		{
			Renderable renderComponent = new()
			{
				_staticMesh = 1,
				_materialCollectionId = MaterialCollectionIDs.GroundTexture
			};
			renderComponent.SetMesh(MeshIDs.Square);

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

					rotatorComponent.Speed = Vector3.Zero;
					world.EntityManager.SetComponent(entity, rotatorComponent);
				}
		}

		private static void CleanUp()
		{
			VulkanEngine.Stop();
			AllocationHandler.CheckUnfreed();
		}
	}
}
