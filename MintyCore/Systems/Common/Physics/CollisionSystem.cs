using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using BulletSharp;
using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Physics;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using BVector3 = BulletSharp.Math.Vector3;
using BMatrix = BulletSharp.Math.Matrix;


namespace MintyCore.Systems.Common.Physics
{
    /// <summary>
    ///     System which adds and removes collision object to the <see cref="World.PhysicsWorld" /> and updates the associated
    ///     <see cref="Entity" />
    /// </summary>
    [ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
    public partial class CollisionSystem : ASystem
    {
        private readonly Stopwatch _physic = new();

        [ComponentQuery] private readonly CollisionQuery<Collider, (Mass, Position, Rotation, Scale)> _query = new();

        [ComponentQuery] private readonly CollisionApplyQuery<(Position, Rotation), Collider> _squery = new();

        /// <summary>
        ///     <see cref="Identification" /> of the <see cref="CollisionSystem" />
        /// </summary>
        public override Identification Identification => SystemIDs.Collision;

        /// <inheritdoc />
        public override void Setup()
        {
            _query.Setup(this);
            _squery.Setup(this);

            _physic.Start();
            EntityManager.PreEntityDeleteEvent += OnEntityDelete;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
        }

        /// <summary>
        ///     Checks if the entity has a rigid body in the physics world and removes it
        /// </summary>
        private void OnEntityDelete(World world, Entity entity)
        {
            //TODO optimize the archetype check
            if (world != World || _query.GetArchetypeStorages().All(x => x.Id != entity.ArchetypeId)) return;

            var collider = World.EntityManager.GetComponent<Collider>(entity);
            if (collider.CollisionObject.NativePtr == IntPtr.Zero) return;
            var colObject = collider.CollisionObject.GetCollisionObject();
            if (colObject is not null && colObject.BroadphaseHandle != null)
                World.PhysicsWorld.RemoveCollisionObject(colObject);
        }

        /// <inheritdoc />
        protected override unsafe void Execute()
        {
            foreach (var entity in _query)
            {
                ref var collider = ref entity.GetCollider();

                if (collider.RemoveFromPhysicsWorld)
                {
                    World.PhysicsWorld.RemoveCollisionObject(collider.CollisionObject);
                    collider.AddedToPhysicsWorld = false;
                    collider.RemoveFromPhysicsWorld = false;
                }

                if (collider.AddedToPhysicsWorld || collider.DontAddToPhysicsWorld) continue;

                if (collider.CollisionShape.NativePtr == IntPtr.Zero)
                {
                    Logger.WriteLog($"Entity {entity} has no valid collision shape", LogImportance.ERROR, "Physics");

                    collider.DontAddToPhysicsWorld = true;
                    continue;
                }

                if (collider.MotionState.NativePtr == IntPtr.Zero)
                {
                    var pos = entity.GetPosition().Value;
                    var rot = entity.GetRotation().Value;
                    var matrix =
                        Matrix4x4.CreateTranslation(pos) *
                        Matrix4x4.CreateFromQuaternion(rot);
                    var motionState = PhysicsObjects.CreateMotionState(*(BMatrix*)&matrix);
                    collider.MotionState = motionState;
                    motionState.Dispose();
                }

                if (collider.CollisionObject.NativePtr == IntPtr.Zero)
                {
                    var massComponent = entity.GetMass();

                    var mass = massComponent.MassValue;
                    var body = PhysicsObjects.CreateRigidBody(mass,
                        collider.MotionState, collider.CollisionShape);
                    collider.CollisionObject = body;
                    body.Dispose();
                }

                World.PhysicsWorld.AddCollisionObject(collider.CollisionObject);
                collider.AddedToPhysicsWorld = true;
            }

            _physic.Stop();
            World.PhysicsWorld.StepSimulation((float)_physic.ElapsedTicks / Stopwatch.Frequency);
            _physic.Restart();

            foreach (var entity in _squery)
            {
                var collider = entity.GetCollider();

                UnsafeNativeMethods.btMotionState_getWorldTransform(collider.MotionState.NativePtr,
                    out var worldTransform);
                Matrix4x4.Decompose(*(Matrix4x4*)&worldTransform, out _, out var rotation, out var translation);
                ref var rot = ref entity.GetRotation();
                ref var pos = ref entity.GetPosition();
                rot.Value = rotation;
                pos.Value = translation;
            }
        }
    }
}