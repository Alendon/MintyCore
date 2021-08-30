using System;
using System.Diagnostics;
using System.Numerics;
using BulletSharp;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Physics;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Utils;
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

        private static unsafe void Run()
        {
            world = new World();

            var playerEntity = world.EntityManager.CreateEntity(ArchetypeIDs.Player, Utils.Constants.ServerID);

            Position playerPos = new Position() { Value = new Vector3(0, 0, 5) };
            world.EntityManager.SetComponent(playerEntity, playerPos);
            //SpawnWallOfDirt();


            renderable = new();
            renderable.SetMesh(MeshIDs.Cube);
            renderable._materialCollectionId = MaterialCollectionIDs.BasicColorCollection;

            Position cubePos = new Position() { Value = new Vector3(0, 0, 0) };

            Mass mass = new Mass();
            mass.MassValue = 1;

            Rotation rotation = new();
            rotation.Value = Quaternion.CreateFromYawPitchRoll(0, 0, 0);

            Transform transform = new();
            transform.PopulateWithDefaultValues();
            
            SpawnCube(renderable, mass, cubePos, transform, CreateBoxCollider(cubePos.Value, rotation.Value, Vector3.One));


            var plane = world.EntityManager.CreateEntity(ArchetypeIDs.RigidBody);
            mass.SetInfiniteMass();

            var scale = new Scale();
            scale.Value = new(100, 1, 100);
            cubePos.Value = new(0, -3, 0);

            var planeCollider = CreateBoxCollider(cubePos.Value, Quaternion.Identity, scale.Value);

            world.EntityManager.SetComponent(plane, renderable);
            world.EntityManager.SetComponent(plane, mass);
            world.EntityManager.SetComponent(plane, scale);
            world.EntityManager.SetComponent(plane, cubePos);
            world.EntityManager.SetComponent(plane, planeCollider);
            
            //The ref count was increased by the entity, so the local reference can be removed
            planeCollider.DecreaseRefCount();


            world.SetupTick();

            tick = Stopwatch.StartNew();
            render = new Stopwatch();
            accumulatedMillis = 0;
            rnd = new Random();
            msDuration = 0.1;

            GameLoopRunning = true;
            while (Window.Exists)
            {
                Mass mass1 = new Mass();
                mass1.MassValue = 7800;
                Position cubePos1 = new();
                Transform transform1 = new();
                if (Tick % 1000 == 0)
                {
                    tick.Stop();
                    //Logger.WriteLog($"Tick duration for the last 100 frames:", LogImportance.INFO, "General", null, true);
                    //Logger.WriteLog($"Complete: {tick.Elapsed.TotalMilliseconds / 100}", LogImportance.INFO, "General", null, true);
                    //Logger.WriteLog($"Rendering: {render.Elapsed.TotalMilliseconds / 100}", LogImportance.INFO, "General", null, true);

                    if (tick.Elapsed.TotalMilliseconds >= msDuration)
                    {
                        Logger.WriteLog(
                            $"Took: {tick.Elapsed.TotalMilliseconds / 1000} for {world.EntityManager.EntityCount}",
                            LogImportance.INFO, "General");
                        msDuration *= 10;
                    }

                    accumulatedMillis += tick.Elapsed.TotalMilliseconds;
                    while (accumulatedMillis > 50)
                    {
                        accumulatedMillis -= 50;
                        float x = rnd.Next(-300, 300) / 100f;
                        float z = rnd.Next(-300, 300) / 100f;
                        float y = 10;
                        cubePos1.Value = new Vector3(x, y, z);
                        transform1.Value = Matrix4x4.CreateTranslation(x, y, z);
                        SpawnCube(renderable, mass1, cubePos1, transform1, CreateBoxCollider(cubePos1.Value, rotation.Value, Vector3.One));
                    }

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

            GameLoopRunning = false;
            world.Dispose();

            Collider CreateBoxCollider(Vector3 position, Quaternion rotationQuaternion, Vector3 scale)
            {
                Collider boxCollider = new Collider();
                BulletSharp.Math.Vector3 colliderScale = *(BulletSharp.Math.Vector3*)&scale / 2;
                BulletSharp.Math.Vector3 colliderPos = *(BulletSharp.Math.Vector3*)&position;
                BulletSharp.Math.Quaternion colliderRot = *(BulletSharp.Math.Quaternion*)&rotationQuaternion;
                
                var collisionShape = PhysicsObjects.CreateBoxShape(colliderScale);
                var motionState = PhysicsObjects.CreateMotionState(BulletSharp.Math.Matrix.Translation(colliderPos) *
                                                                   BulletSharp.Math.Matrix.RotationQuaternion(
                                                                       colliderRot));

                boxCollider.CollisionShape = collisionShape;
                boxCollider.MotionState = motionState;
                
                //The reference is automatically "taken" by the created collider, so the local reference can be "disposed"
                collisionShape.Dispose();
                motionState.Dispose();
                
                return boxCollider;
            }
        }

        internal static bool GameLoopRunning = false;
        static Stopwatch tick;
        static Stopwatch render;
        static double accumulatedMillis;
        static Random rnd;
        static double msDuration;

        private static Renderable renderable;

        private static void SpawnCube(Renderable renderable, Mass mass, Position cubePos, Transform transform, Collider collider)
        {
            var physicsCube = world.EntityManager.CreateEntity(ArchetypeIDs.RigidBody);
            mass.MassValue = 7800;
            world.EntityManager.SetComponent(physicsCube, renderable);
            world.EntityManager.SetComponent(physicsCube, mass);
            world.EntityManager.SetComponent(physicsCube, cubePos);
            world.EntityManager.SetComponent(physicsCube, transform);
            world.EntityManager.SetComponent(physicsCube, collider);
            
            collider.DecreaseRefCount();
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
            BulletObjectTracker tracker = BulletObjectTracker.Current;
            if (tracker.GetUserOwnedObjects().Count != 0)
                Logger.WriteLog($"{tracker.GetUserOwnedObjects().Count} BulletObjects were not disposed",
                    LogImportance.WARNING, "Physics");
            else
                Logger.WriteLog("All BulletObjects were disposed", LogImportance.INFO, "Physics");
            
            
            VulkanEngine.Stop();
            AllocationHandler.CheckUnfreed();
        }
    }
}