using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using MintyCore;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Input;

namespace TestMod
{
    public class SimpleTestMod : IMod
    {
        public void Dispose()
        {
        }

        public ushort ModId { get; set; }
        public string StringIdentifier => "test";
        public string ModDescription => "Just a mod to test the ModManager";
        public string ModName => "Test Mod";

        public ModVersion ModVersion => new ModVersion(0, 0, 1);
        public ModDependency[] ModDependencies => Array.Empty<ModDependency>();
        public GameType ExecutionSide => GameType.LOCAL;

        public static int RandomNumber;

        public void PreLoad()
        {
            Random rnd = new();
            RandomNumber = rnd.Next(1, 1000);
            Logger.WriteLog($"Generated Number: {RandomNumber}", LogImportance.INFO, "TestMod");
        }

        public void Load()
        {
            Logger.WriteLog("Loaded", LogImportance.INFO, "TestMod");

            ArchetypeRegistry.OnRegister += RegisterArchetypes;

            Engine.OnServerWorldCreate += CreatePhysicEntities;
            Engine.OnPlayerConnected += SpawnPlayerCamera;
            Engine.AfterWorldTicking += SpawnNewCube;
        }

        private int _spawnCount = 10;


        private static bool _lastFrameSDown;

        private void SpawnNewCube()
        {
            if (Engine.ServerWorld is null) return;
            var entityManager = Engine.ServerWorld.EntityManager;

            int spawned = 0;

            if (InputHandler.GetKeyDown(Key.Up))
            {
                _spawnCount++;
                Console.WriteLine(_spawnCount);
            }

            if (InputHandler.GetKeyDown(Key.Down))
            {
                _spawnCount--;
                Console.WriteLine(_spawnCount);
            }

            bool sDown = InputHandler.GetKeyDown(Key.S);

            if (!_lastFrameSDown && sDown)
            {
                int sqrt = (int)MathF.Sqrt(_spawnCount);
                int start = -sqrt / 2;
                int end = sqrt / 2;

                for (int x = start*2; x < end*2; x+=2)
                for (int y = start*2; y < end*2; y+=2)
                for (int z = start*2; z < end*2; z+=2)
                {
                    entityManager.CreateEntity(PhysicBoxArchetype,
                        new PhysicBoxSetup() { Mass = 10, Position = new Vector3(x, y + 20, z), Scale = Vector3.One });
                    spawned++;
                }

                Logger.WriteLog($"{spawned} spawned", LogImportance.INFO, "TestMod");
            }

            _lastFrameSDown = sDown;
        }

        private void CreatePhysicEntities()
        {
            if (Engine.ServerWorld is null) return;

            var entityManager = Engine.ServerWorld.EntityManager;

            Vector3 scale = new Vector3(100, 1, 100);

            entityManager.CreateEntity(PhysicBoxArchetype,
                new PhysicBoxSetup() { Mass = 0, Position = Vector3.Zero, Scale = scale });

            entityManager.CreateEntity(PhysicBoxArchetype,
                new PhysicBoxSetup() { Mass = 10, Position = new Vector3(0, 10, 0), Scale = Vector3.One });
            entityManager.CreateEntity(PhysicBoxArchetype,
                new PhysicBoxSetup() { Mass = 10, Position = new Vector3(0, 1, 0), Scale = Vector3.One });
            entityManager.CreateEntity(PhysicBoxArchetype,
                new PhysicBoxSetup() { Mass = 10, Position = new Vector3(0, 3, 0), Scale = Vector3.One });
        }

        private void SpawnPlayerCamera(ushort playerGameId, bool serverside)
        {
            if (Engine.ServerWorld is null || !serverside) return;

            var entity = Engine.ServerWorld.EntityManager.CreateEntity(CameraArchetype, null, playerGameId);
            Engine.ServerWorld.EntityManager.SetComponent(entity, new Position { Value = new(0, 5, -20) });
        }

        public void PostLoad()
        {
        }

        public void Unload()
        {
            Logger.WriteLog("Unloaded", LogImportance.INFO, "TestMod");
        }

        public static Identification CameraArchetype;
        public static Identification PhysicBoxArchetype;
        public static Identification EntitySetup;

        public void RegisterArchetypes()
        {
            ArchetypeContainer camera = new(ComponentIDs.Camera, ComponentIDs.Position);
            ArchetypeContainer physicBox = new ArchetypeContainer(ComponentIDs.Position, ComponentIDs.Rotation,
                ComponentIDs.Scale, ComponentIDs.Transform, ComponentIDs.Mass, ComponentIDs.Collider,
                ComponentIDs.InstancedRenderAble);

            CameraArchetype = ArchetypeRegistry.RegisterArchetype(camera, ModId, "camera");
            PhysicBoxArchetype =
                ArchetypeRegistry.RegisterArchetype(physicBox, ModId, "physic_box", new PhysicBoxSetup());
        }

        class PhysicBoxSetup : IEntitySetup
        {
            public float Mass;
            public Vector3 Position;
            public Vector3 Scale;

            public void GatherEntityData(World world, Entity entity)
            {
                Mass = world.EntityManager.GetComponent<Mass>(entity).MassValue;
                Position = world.EntityManager.GetComponent<Position>(entity).Value;
                Scale = world.EntityManager.GetComponent<Scale>(entity).Value;
            }

            public void SetupEntity(World world, Entity entity)
            {
                world.EntityManager.SetComponent(entity, new Mass { MassValue = Mass }, false);
                world.EntityManager.SetComponent(entity, new Position { Value = Position });
                world.EntityManager.SetComponent(entity, new Scale { Value = Scale }, false);

                RigidPose pose = new RigidPose(Position, Quaternion.Identity);
                Box shape = new Box(Scale.X, Scale.Y, Scale.Z);
                BodyInertia inertia = default;

                if (Mass != 0)
                {
                    inertia = shape.ComputeInertia(Mass);
                }

                var description = BodyDescription.CreateDynamic(pose, inertia,
                    new CollidableDescription(world.PhysicsWorld.AddShape(shape), 10),
                    new BodyActivityDescription(0.1f));

                var handle = world.PhysicsWorld.AddBody(description);
                world.EntityManager.SetComponent(entity, new Collider { BodyHandle = handle }, false);

                InstancedRenderAble boxRender = new InstancedRenderAble()
                {
                    MaterialMeshCombination = InstancedRenderDataIDs.Testing
                };

                world.EntityManager.SetComponent(entity, boxRender);
            }

            public void Serialize(DataWriter writer)
            {
                writer.Put(Mass);
                writer.Put(Position);
                writer.Put(Scale);
            }

            public void Deserialize(DataReader reader)
            {
                Mass = reader.GetFloat();
                Position = reader.GetVector3();
                Scale = reader.GetVector3();
            }
        }
    }
}