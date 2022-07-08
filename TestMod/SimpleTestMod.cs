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
using static MintyCore.Registries.KeyActionInfo;

namespace TestMod
{
    public partial class SimpleTestMod : IMod
    {
        public static int RandomNumber;


        private static bool _lastFrameSDown;

        private int _spawnCount = 10;

        public static SimpleTestMod? Instance;

        public void Dispose()
        {
        }

        public ushort ModId { get; set; }
        public string StringIdentifier => "test";
        public string ModDescription => "Just a mod to test the ModManager";
        public string ModName => "Test Mod";

        public ModVersion ModVersion => new(0, 0, 1);
        public ModDependency[] ModDependencies => Array.Empty<ModDependency>();
        public GameType ExecutionSide => GameType.Local;

        public void PreLoad()
        {
            Instance = this;

            Random rnd = new();
            RandomNumber = rnd.Next(1, 1000);
            Logger.WriteLog($"Generated Number: {RandomNumber}", LogImportance.Info, "TestMod");
        }

        public void Load()
        {
            InternalRegister();

            WorldHandler.OnWorldCreate += CreatePhysicEntities;
            PlayerHandler.OnPlayerConnected += SpawnPlayerCamera;
            WorldHandler.AfterWorldUpdate += SpawnNewCube;

            Logger.WriteLog("Loaded", LogImportance.Info, "TestMod");
        }

        public void PostLoad()
        {
        }

        public void Unload()
        {
            InternalUnregister();

            WorldHandler.OnWorldCreate -= CreatePhysicEntities;
            PlayerHandler.OnPlayerConnected -= SpawnPlayerCamera;
            WorldHandler.AfterWorldUpdate -= SpawnNewCube;

            Logger.WriteLog("Unloaded", LogImportance.Info, "TestMod");
        }

        private void SpawnNewCube(IWorld world1)
        {
            if (!WorldHandler.TryGetWorld(GameType.Server, WorldIDs.Default, out var world)) return;

            var entityManager = world.EntityManager;

            var spawned = 0;

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

            var sDown = InputHandler.GetKeyDown(Key.S);

            if (!_lastFrameSDown && sDown)
            {
                var sqrt = (int) MathF.Sqrt(_spawnCount);
                var start = -sqrt / 2;
                var end = sqrt / 2;

                for (var x = start * 2; x < end * 2; x += 2)
                for (var y = start * 2; y < end * 2; y += 2)
                for (var z = start * 2; z < end * 2; z += 2)
                {
                    entityManager.CreateEntity(Identifications.ArchetypeIDs.PhysicBox, null,
                        new PhysicBoxSetup {Mass = 10, Position = new Vector3(x, y + 20, z), Scale = Vector3.One});
                    spawned++;
                }

                Logger.WriteLog($"{spawned} spawned", LogImportance.Info, "TestMod");
            }

            _lastFrameSDown = sDown;
        }

        private void CreatePhysicEntities(IWorld world)
        {
            if (!world.IsServerWorld || world.Identification != WorldIDs.Default) return;

            var entityManager = world.EntityManager;

            var scale = new Vector3(100, 1, 100);

            entityManager.CreateEntity(Identifications.ArchetypeIDs.PhysicBox, null,
                new PhysicBoxSetup {Mass = 0, Position = Vector3.Zero, Scale = scale});

            entityManager.CreateEntity(Identifications.ArchetypeIDs.PhysicBox, null,
                new PhysicBoxSetup {Mass = 10, Position = new Vector3(0, 10, 0), Scale = Vector3.One});
            entityManager.CreateEntity(Identifications.ArchetypeIDs.PhysicBox, null,
                new PhysicBoxSetup {Mass = 10, Position = new Vector3(0, 1, 0), Scale = Vector3.One});
            entityManager.CreateEntity(Identifications.ArchetypeIDs.PhysicBox, null,
                new PhysicBoxSetup {Mass = 10, Position = new Vector3(0, 3, 0), Scale = Vector3.One});
        }

        private void SpawnPlayerCamera(Player player, bool serverside)
        {
            if (!serverside || !WorldHandler.TryGetWorld(GameType.Server, WorldIDs.Default, out var world)) return;

            var entity = world.EntityManager.CreateEntity(Identifications.ArchetypeIDs.Camera, player.GameId);
            world.EntityManager.SetComponent(entity, new Position {Value = new Vector3(0, 5, -20)});
        }

        [RegisterArchetype("camera")]
        internal static ArchetypeInfo camera => new(new[] {ComponentIDs.Camera, ComponentIDs.Position}, null, new[]
        {
            typeof(Silk.NET.Vulkan.DescriptorSet).Assembly.Location
        });

        [RegisterArchetype("physic_box")]
        internal static ArchetypeInfo physicBox => new(new[]
        {
            ComponentIDs.Position, ComponentIDs.Rotation,
            ComponentIDs.Scale, ComponentIDs.Transform, ComponentIDs.Mass, ComponentIDs.Collider,
            ComponentIDs.InstancedRenderAble
        }, new PhysicBoxSetup(), new[]
        {
            typeof(BepuPhysics.BodyHandle).Assembly.Location
        });

        [RegisterKeyAction("TestKeyActionDown")]
        public static KeyActionInfo TestKeyActionDown => new KeyActionInfo()
        {
            Key = Key.E,
            Action = delegate { Logger.WriteLog("TestKeyActionDown Triggered", LogImportance.Debug, "TestMod"); },

            KeyStatus = KeyStatus.KeyDown
        };

        [RegisterKeyAction("TestKeyActionUp")]
        public static KeyActionInfo TestKeyActionUp => new KeyActionInfo()
        {
            Key = Key.E,
            Action = delegate { Logger.WriteLog("TestKeyActionUp Triggered", LogImportance.Debug, "TestMod"); },

            KeyStatus = KeyStatus.KeyUp
        };

        [RegisterKeyAction("MouseTestKeyActionDown")]
        public static KeyActionInfo TestMouseButtonActionDown => new()
        {
            MouseButton = MouseButton.Left,
            Action = delegate { Logger.WriteLog("Left mouse button Pressed", LogImportance.Debug, "TestMod"); },

            MouseButtonStatus = MouseButtonStatus.MouseButtonDown
        };

        [RegisterKeyAction("MouseTestKeyActionUp")]
        public static KeyActionInfo TestMouseButtonActionUp => new()
        {
            MouseButton = MouseButton.Left,
            Action = delegate { Logger.WriteLog("Left mouse button Pressed", LogImportance.Debug, "TestMod"); },

            MouseButtonStatus = MouseButtonStatus.MouseButtonUp
        };

        class PhysicBoxSetup : IEntitySetup
        {
            public float Mass;
            public Vector3 Position;
            public Vector3 Scale;

            public void GatherEntityData(IWorld world, Entity entity)
            {
                Mass = world.EntityManager.GetComponent<Mass>(entity).MassValue;
                Position = world.EntityManager.GetComponent<Position>(entity).Value;
                Scale = world.EntityManager.GetComponent<Scale>(entity).Value;
            }

            public void SetupEntity(IWorld world, Entity entity)
            {
                world.EntityManager.SetComponent(entity, new Mass {MassValue = Mass}, false);
                world.EntityManager.SetComponent(entity, new Position {Value = Position});
                world.EntityManager.SetComponent(entity, new Scale {Value = Scale}, false);

                var pose = new RigidPose(Position, Quaternion.Identity);
                var shape = new Box(Scale.X, Scale.Y, Scale.Z);
                BodyInertia inertia = default;

                if (Mass != 0) inertia = shape.ComputeInertia(Mass);

                var description = BodyDescription.CreateDynamic(pose, inertia,
                    new CollidableDescription(world.PhysicsWorld.AddShape(shape), 10),
                    new BodyActivityDescription(0.1f));

                var handle = world.PhysicsWorld.AddBody(description);
                world.EntityManager.SetComponent(entity, new Collider {BodyHandle = handle}, false);

                var boxRender = new InstancedRenderAble
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

            public bool Deserialize(DataReader reader)
            {
                if (!reader.TryGetFloat(out var mass)
                    || !reader.TryGetVector3(out var position)
                    || !reader.TryGetVector3(out var scale))
                    return false;

                Mass = mass;
                Position = position;
                Scale = scale;
                return true;
            }
        }
    }
}