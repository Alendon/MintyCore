using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BulletSharp;
using BulletSharp.Math;
using MintyCore.ECS;
using MintyCore.Physics.Native;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Physics
{
    public class PhysicsWorld : IDisposable
    {
        private CollisionConfiguration _collisionConfiguration;
        private CollisionDispatcher _collisionDispatcher;
        private DbvtBroadphase _broadphase;
        public readonly DiscreteDynamicsWorld _world;
        
        

        private World _parent;

        public PhysicsWorld(World parent)
        {
            _parent = parent;
            _collisionConfiguration = new DefaultCollisionConfiguration();
            _collisionDispatcher = new CollisionDispatcher(_collisionConfiguration);
            _broadphase = new DbvtBroadphase();
            _world = new DiscreteDynamicsWorld(_collisionDispatcher, _broadphase, null, _collisionConfiguration);
        }

        public unsafe NativeMotionState CreateMotionState(Matrix transform, Matrix centerOfMassOffset = default)
        {
            if (centerOfMassOffset == default)
            {
                var identityMatrix = System.Numerics.Matrix4x4.Identity;
                centerOfMassOffset = *(Matrix*)&identityMatrix;
            }

            var motionState = new DefaultMotionState(transform, centerOfMassOffset);
            var handle = GCHandle.Alloc(motionState, GCHandleType.Normal);
            motionState.UserPointer = handle.AddrOfPinnedObject();
            
            //TODO
            return default;
        }

        public unsafe NativeCollisionShape CreateCapsuleShape(float radius, float height)
        {
            var shape = new CapsuleShape(radius, height);
            var disposer = new UnmanagedDisposer(&DisposeCollisionShape, shape.Native);
            return new NativeCollisionShape(shape.Native, disposer);
        }

        public unsafe NativeCollisionShape CreateBoxShape(Vector3 halfExtent)
        {
            var shape = new BoxShape(halfExtent);
            var disposer = new UnmanagedDisposer(&DisposeCollisionShape, shape.Native);
            return new NativeCollisionShape(shape.Native, disposer);
        }
        
        private static void DisposeCollisionShape(IntPtr collisionObject)
        {
            var colObject = CollisionShape.GetManaged(collisionObject);
            
            //Check if the original managed collision shape is still alive
            //This will be more likely the case in Debug build as there the bullet object tracker is enabled
            if (colObject is not null)
            {
                //And use the "official" dispose method
                colObject.Dispose();
                return;
            }
            
            //Otherwise only delete the native collision shape
            UnsafeNativeMethods.btCollisionShape_delete(collisionObject);
        }
        
        public unsafe NativeCollisionObject CreateRigidBody(RigidBodyConstructionInfo constructionInfo, bool addToWorld = true)
        {
            var body = new RigidBody(constructionInfo);
            var disposer = new UnmanagedDisposer(&DisposeCollisionObject, body.Native);
            if(addToWorld) _world.AddRigidBody(body);
            return new NativeCollisionObject(body.Native, disposer);
        }
        
        public unsafe NativeCollisionObject CreateRigidBody(float mass, MotionState motionState, CollisionShape collisionShape, bool addToWorld = true)
        {
            var constructionInfo = new RigidBodyConstructionInfo(mass, motionState, collisionShape);
            var body = new RigidBody(constructionInfo);
            var disposer = new UnmanagedDisposer(&DisposeCollisionObject, body.Native);
            if(addToWorld) _world.AddRigidBody(body);
            return new NativeCollisionObject(body.Native, disposer);
        }
        
        public unsafe NativeCollisionObject CreateRigidBody(float mass, NativeMotionState motionState, NativeCollisionShape collisionShape, bool addToWorld = true)
        {
            var constructionInfo = new RigidBodyConstructionInfo(mass, motionState.GetMotionState(), collisionShape.GetCollisionShape() );
            var body = new RigidBody(constructionInfo);
            var disposer = new UnmanagedDisposer(&DisposeCollisionObject, body.Native);
            if(addToWorld) _world.AddRigidBody(body);
            return new NativeCollisionObject(body.Native, disposer);
        }
        
        private static void DisposeCollisionObject(IntPtr collisionObject)
        {
            var colObject = CollisionObject.GetManaged(collisionObject);
            
            //Check if the original managed collision object is still alive
            //This will be more likely the case in Debug build as there the bullet object tracker is enabled
            if (colObject is not null)
            {
                //And use the "official" dispose method
                colObject.Dispose();
                return;
            }
            
            //Otherwise only delete the native collision object
            UnsafeNativeMethods.btCollisionObject_delete(collisionObject);
        }

        public void Dispose()
        {
            List<CollisionObject> toRemove = new(_world.NumCollisionObjects);
            foreach (var collisionObject in _world.CollisionObjectArray)
            {
                toRemove.Add(collisionObject);
            }

            foreach (var collisionObject in toRemove)
            {
                _world.RemoveCollisionObject(collisionObject);
                if (collisionObject is RigidBody rigidBody)
                {
                    rigidBody.MotionState.Dispose();
                }
                collisionObject.CollisionShape.Dispose();
                collisionObject.Dispose();
            }
            
            _collisionConfiguration.Dispose();
            _collisionDispatcher.Dispose();
            _broadphase.Dispose();
            _world.Dispose();
        }
    }
}