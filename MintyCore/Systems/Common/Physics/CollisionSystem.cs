using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using BulletSharp;
using BVector3 = BulletSharp.Math.Vector3;
using BMatrix = BulletSharp.Math.Matrix;
using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Physics;
using MintyCore.SystemGroups;
using MintyCore.Utils;


namespace MintyCore.Systems.Common.Physics
{
    /// <summary>
    /// System which adds and removes collision object to the <see cref="World.PhysicsWorld"/> and updates the associated <see cref="Entity"/>
    /// </summary>
    [ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
    public partial class CollisionSystem : ASystem
    {
        /// <summary>
        /// <see cref="Identification"/> of the <see cref="CollisionSystem"/>
        /// </summary>
        public override Identification Identification => SystemIDs.Collision;

        [ComponentQuery] private CollisionQuery<Collider, (Mass, Position, Rotation, Scale)> _query = new();

        [ComponentQuery] private CollisionApplyQuery<(Position, Rotation), Collider> _squery = new();

        private Stopwatch physic = new();

        /// <inheritdoc />
        public override void Setup()
        {
            _query.Setup(this);
            _squery.Setup(this);

            physic.Start();
            EntityManager.PreEntityDeleteEvent += OnEntityDelete;
        }
        
        /// <inheritdoc/>
        public override void Dispose()
        {
        }

        /// <summary>
        /// Checks if the entity has a rigid body in the physics world and removes it
        /// </summary>
        private void OnEntityDelete(World world, Entity entity)
        {
            //TODO optimize the archetype check
            if (world != World || _query.GetArchetypeStorages().All(x => x.ID != entity.ArchetypeID)) return;

            Collider collider = World.EntityManager.GetComponent<Collider>(entity);
            if (collider.CollisionObject.NativePtr == IntPtr.Zero) return;
            CollisionObject colObject = collider.CollisionObject.GetCollisionObject();
            if (colObject is not null && colObject.BroadphaseHandle != null)
            {
                World.PhysicsWorld.RemoveCollisionObject(colObject);
            }
        }

        /// <inheritdoc />
        public override unsafe void Execute()
        {
            foreach (var entity in _query)
            {
                ref Collider collider = ref entity.GetCollider();

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
                    Matrix4x4 matrix =
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

            physic.Stop();
            World.PhysicsWorld.StepSimulation((float)physic.ElapsedTicks / Stopwatch.Frequency);
            physic.Restart();

            foreach (var entity in _squery)
            {
                Collider collider = entity.GetCollider();

                UnsafeNativeMethods.btMotionState_getWorldTransform(collider.MotionState.NativePtr,
                    out BMatrix worldTransform);
                Matrix4x4.Decompose(*(Matrix4x4*)&worldTransform, out _, out var rotation, out var translation);
                ref Rotation rot = ref entity.GetRotation();
                ref Position pos = ref entity.GetPosition();
                rot.Value = rotation;
                pos.Value = translation;
            }
        }
    }
}