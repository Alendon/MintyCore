using BulletSharp;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Physics.Native;
using MintyCore.Utils;
using NMatrix = System.Numerics.Matrix4x4;
using BMatrix = BulletSharp.Math.Matrix;

namespace MintyCore.Components.Common.Physic
{
    /// <summary>
    ///     Holds all relevant collider data
    /// </summary>
    public struct Collider : IComponent
    {
        /// <inheritdoc />
        public byte Dirty { get; set; }

        /// <summary>
        ///     <see cref="Identification" /> of the <see cref="Collider" /> Component
        /// </summary>
        public Identification Identification => ComponentIDs.Collider;

        private NativeCollisionObject _collisionObject;
        private NativeCollisionShape _collisionShape;
        private NativeMotionState _motionState;

        private byte _addedToPhysicsWorld;
        private byte _removeFromPhysicsWorld;
        private byte _dontAddToPhysicsWorld;

        /// <summary>
        ///     Get or set the <see cref="NativeCollisionObject" />
        ///     Will increase the ReferenceCount of the new one and decrease the refCount of the old  one
        /// </summary>
        public NativeCollisionObject CollisionObject
        {
            get => _collisionObject;
            set
            {
                _collisionObject.Dispose();
                _collisionObject = value;
                _collisionObject.IncreaseReferenceCount();
            }
        }

        /// <summary>
        ///     Get or set the <see cref="NativeCollisionShape" />
        ///     Will increase the ReferenceCount of the new one and decrease the refCount of the old  one
        /// </summary>
        public NativeCollisionShape CollisionShape
        {
            get => _collisionShape;
            set
            {
                _collisionShape.Dispose();
                _collisionShape = value;
                _collisionShape.IncreaseReferenceCount();
            }
        }

        /// <summary>
        ///     Get or set the <see cref="NativeMotionState" />
        ///     Will increase the ReferenceCount of the new one and decrease the refCount of the old  one
        /// </summary>
        public NativeMotionState MotionState
        {
            get => _motionState;
            set
            {
                _motionState.Dispose();
                _motionState = value;
                _motionState.IncreaseReferenceCount();
            }
        }

        /// <summary>
        ///     Is the collider added to a physics world
        /// </summary>
        public bool AddedToPhysicsWorld
        {
            get => _addedToPhysicsWorld != 0;
            set => _addedToPhysicsWorld = value ? (byte)1 : (byte)0;
        }

        /// <summary>
        ///     Should the collider be removed from the physics world
        /// </summary>
        public bool RemoveFromPhysicsWorld
        {
            get => _removeFromPhysicsWorld != 0;
            set => _removeFromPhysicsWorld = value ? (byte)1 : (byte)0;
        }

        /// <summary>
        ///     Should the collider dont be added to a physics world (the collider will not be removed if its already present in
        ///     one)
        /// </summary>
        public bool DontAddToPhysicsWorld
        {
            get => _dontAddToPhysicsWorld != 0;
            set => _dontAddToPhysicsWorld = value ? (byte)1 : (byte)0;
        }

        /// <summary>
        ///     Deserialize only the <see cref="MotionState" />
        /// </summary>
        public unsafe void Deserialize(DataReader reader)
        {
            var matrix = reader.GetMatrix4X4();
            var bMatrix = *(BMatrix*)&matrix;
            UnsafeNativeMethods.btMotionState_setWorldTransform(_motionState.NativePtr, ref bMatrix);
        }

        /// <inheritdoc />
        public void DecreaseRefCount()
        {
            _motionState.Dispose();
            _collisionObject.Dispose();
            _collisionShape.Dispose();
        }

        /// <inheritdoc />
        public void IncreaseRefCount()
        {
            _motionState.IncreaseReferenceCount();
            _collisionObject.IncreaseReferenceCount();
            _collisionShape.IncreaseReferenceCount();
        }

        /// <inheritdoc />
        public void PopulateWithDefaultValues()
        {
            _collisionObject = default;
            _collisionShape = default;
            _motionState = default;
            _addedToPhysicsWorld = 0;
            _removeFromPhysicsWorld = 0;
            _dontAddToPhysicsWorld = 0;
        }

        /// <summary>
        ///     Serialize only the <see cref="MotionState" />
        /// </summary>
        public unsafe void Serialize(DataWriter writer)
        {
            UnsafeNativeMethods.btMotionState_getWorldTransform(_motionState.NativePtr, out var matrix);
            writer.Put(*(NMatrix*)&matrix);
        }
    }
}