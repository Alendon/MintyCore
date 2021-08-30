using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Physics.Native;
using MintyCore.Utils;

namespace MintyCore.Components.Common.Physic
{
    public struct Collider : IComponent
    {
        public byte Dirty { get; set; }

        public Identification Identification => ComponentIDs.Collider;

        private NativeCollisionObject _collisionObject;
        private NativeCollisionShape _collisionShape;
        private NativeMotionState _motionState;

        private byte _addedToPhysicsWorld;
        private byte _removeFromPhysicsWorld;
        private byte _dontAddToPhysicsWorld;

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

        public bool AddedToPhysicsWorld
        {
            get => _addedToPhysicsWorld != 0;
            set => _addedToPhysicsWorld = value ? (byte)1 : (byte)0;
        }

        public bool RemoveFromPhysicsWorld
        {
            get => _removeFromPhysicsWorld != 0;
            set => _removeFromPhysicsWorld = value ? (byte)1 : (byte)0;
        }

        public bool DontAddToPhysicsWorld
        {
            get => _dontAddToPhysicsWorld != 0;
            set => _dontAddToPhysicsWorld = value ? (byte)1 : (byte)0;
        }


        public void Deserialize(DataReader reader)
        {
        }


        public void DecreaseRefCount()
        {
            _motionState.Dispose();
            _collisionObject.Dispose();
            _collisionShape.Dispose();
        }

        public void IncreaseRefCount()
        {
            _motionState.IncreaseReferenceCount();
            _collisionObject.IncreaseReferenceCount();
            _collisionShape.IncreaseReferenceCount();
        }

        public void PopulateWithDefaultValues()
        {
            _collisionObject = default;
            _collisionShape = default;
            _motionState = default;
            _addedToPhysicsWorld = 0;
            _removeFromPhysicsWorld = 0;
            _dontAddToPhysicsWorld = 0;
        }

        public void Serialize(DataWriter writer)
        {
        }
    }
}