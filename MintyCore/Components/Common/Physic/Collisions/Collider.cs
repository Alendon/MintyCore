using System;
using System.Numerics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Physics.Native;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Components.Common.Physic.Collisions
{
    public struct Collider : IComponent
    {
        public byte Dirty { get; set; }

        public Identification Identification => ComponentIDs.Collider;

        private NativeCollisionObject _collisionObject;
        private NativeCollisionShape _collisionShape;
        private NativeMotionState _motionState;

        public IntPtr nativeBody;
        public IntPtr nativeMotionState;
        
        public NativeCollisionObject CollisionObject
        {
            get => _collisionObject;
            set
            {
                _collisionObject._disposer.DecreaseRefCount();
                _collisionObject = value;
                _collisionObject._disposer.IncreaseRefCount();
            }
        }

        public NativeCollisionShape CollisionShape
        {
            get => _collisionShape;
            set
            {
                _collisionShape._disposer.DecreaseRefCount();
                _collisionShape = value;
                _collisionShape._disposer.IncreaseRefCount();
            }
        }

        public NativeMotionState MotionState
        {
            get => _motionState;
            set
            {
                _motionState._disposer.DecreaseRefCount();
                _motionState = value;
                _motionState._disposer.IncreaseRefCount();
            }
        }


        public void Deserialize(DataReader reader)
        {
        }


        public void DecreaseRefCount()
        {
            _motionState._disposer.DecreaseRefCount();
            _collisionObject._disposer.DecreaseRefCount();
            _collisionShape._disposer.DecreaseRefCount();
        }

        public void IncreaseRefCount()
        {
            _motionState._disposer.IncreaseRefCount();
            _collisionObject._disposer.IncreaseRefCount();
            _collisionShape._disposer.IncreaseRefCount();
        }

        public void PopulateWithDefaultValues()
        {
            _collisionObject = default;
            _collisionShape = default;
            _motionState = default;
        }

        public void Serialize(DataWriter writer)
        {
        }
    }
}