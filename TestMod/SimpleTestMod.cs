using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using MintyCore;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;

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

        public static int randomNumber;

        public void PreLoad()
        {
            Random rnd = new();
            randomNumber = rnd.Next(1, 1000);
            Logger.WriteLog($"Generated Number: {randomNumber}", LogImportance.INFO, "TestMod");
        }

        public void Load()
        {
            Logger.WriteLog("Loaded", LogImportance.INFO, "TestMod");

            ArchetypeRegistry.OnRegister += RegisterArchetypes;
            Engine.OnServerWorldCreate += CreatePhysicEntities;
            Engine.OnPlayerConnected += SpawnPlayerCamera;
            Engine.AfterWorldTicking += SpawnNewCube;
        }

        private Random rnd = new();

        private int spawnCount = 10;

        private void SpawnNewCube()
        {
            if (Engine.ServerWorld is null) return;
            var entityManager = Engine.ServerWorld.EntityManager;

            int spawned = 0;

            if (InputHandler.GetKeyEvent(Key.Up).Down)
            {
                spawnCount++;
                Console.WriteLine(spawnCount);
            }

            if (InputHandler.GetKeyEvent(Key.Down).Down)
            {
                spawnCount--;
                Console.WriteLine(spawnCount);
            }
            
            if (InputHandler.GetKeyEvent(Key.S).Down)
            {
                for (int i = 0; i < spawnCount; i++)
                {
                    float x = rnd.Next(-500, 500) / 100f;
                    float z = rnd.Next(-500, 500) / 100f;
                    float y = 20 + rnd.Next(-500, 500) / 100f;
                    entityManager.CreateEntity(PhysicBoxArchetype,
                        new PhysicBoxSetup() { Mass = 10, Position = new Vector3(x, y, z), Scale = Vector3.One });
                    spawned++;
                }
                Logger.WriteLog($"{spawned} spawned", LogImportance.INFO, "TestMod");
            }

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
                ComponentIDs.Renderable);

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
                world.EntityManager.SetComponent(entity, new Position { Value = Position }, false);
                world.EntityManager.SetComponent(entity, new Scale { Value = Scale }, false);

                RigidPose pose = new RigidPose(Position, Quaternion.Identity);
                Box shape = new Box(Scale.X, Scale.Y, Scale.Z);
                BodyInertia inertia = default;

                if (Mass != 0)
                {
                    shape.ComputeInertia(Mass, out inertia);
                }

                var description = BodyDescription.CreateDynamic(pose, inertia,
                    new CollidableDescription(world.PhysicsWorld.AddShape(shape), 10),
                    new BodyActivityDescription(0.01f));

                var handle = world.PhysicsWorld.AddBody(description);
                world.EntityManager.SetComponent(entity, new Collider { BodyHandle = handle }, false);

                RenderAble boxRender = default;
                UnmanagedArray<GCHandle> materialArray = new UnmanagedArray<GCHandle>(1);
                materialArray[0] = MaterialHandler.GetMaterialHandle(MaterialIDs.Color);

                boxRender.SetMesh(MeshHandler.GetStaticMeshHandle(MeshIDs.Cube));
                boxRender.SetMaterials(materialArray);

                world.EntityManager.SetComponent(entity, boxRender, false);
                materialArray.DecreaseRefCount();
                boxRender.DecreaseRefCount();
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