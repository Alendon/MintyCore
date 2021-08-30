using System;
using System.Collections.Generic;
using BulletSharp;
using MintyCore.ECS;
using MintyCore.Physics.Native;

namespace MintyCore.Physics
{
    /// <summary>
    /// Holds all relevant data and logic to simulate and interact with a physics world
    /// </summary>
    public class PhysicsWorld : IDisposable
    {
        private readonly CollisionConfiguration _collisionConfiguration;
        private readonly CollisionDispatcher _collisionDispatcher;
        private readonly DbvtBroadphase _broadphase;
        private readonly DiscreteDynamicsWorld _world;


        private readonly World _parent;

        /// <summary>
        /// Create a new physics world
        /// </summary>
        public PhysicsWorld(World parent)
        {
            _parent = parent;
            _collisionConfiguration = new DefaultCollisionConfiguration();
            _collisionDispatcher = new CollisionDispatcher(_collisionConfiguration);
            _broadphase = new DbvtBroadphase();
            _world = new DiscreteDynamicsWorld(_collisionDispatcher, _broadphase, null, _collisionConfiguration);
        }

        public void StepSimulation(float timestep)
        {
            _world.StepSimulation(timestep);
        }
        
        /// <summary>
        /// Remove a collision object from the world
        /// </summary>
        public void RemoveCollisionObject(NativeCollisionObject nativeCollisionObject)
        {
            var collisionObject = nativeCollisionObject.GetCollisionObject();
            //As the collision world holds a reference to all managed collision objects,
            //the collision object can not be a part of it if its null
            if (collisionObject is not null) RemoveCollisionObject(collisionObject);
        }

        /// <summary>
        /// Remove a collision object from the world
        /// </summary>
        public void RemoveCollisionObject(CollisionObject collisionObject)
        {
            _world.RemoveCollisionObject(collisionObject);
        }
        
        /// <summary>
        /// Add a collision object to the world
        /// </summary>
        public void AddCollisionObject(NativeCollisionObject nativeCollisionObject)
        {
            CollisionObject collisionObject = nativeCollisionObject.GetCollisionObject() ??
                                              new CollisionObject(ConstructionInfo.Null)
                                                  { Native = nativeCollisionObject.NativePtr };
            AddCollisionObject(collisionObject);
        }

        /// <summary>
        /// Add a collision object to the world
        /// </summary>
        public void AddCollisionObject(CollisionObject collisionObject)
        {
            _world.AddCollisionObject(collisionObject);
        }

        /// <summary>
        /// Removes and deletes all remaining <see cref="CollisionObject"/>
        /// and disposes all data for the physics simulation of this world
        /// </summary>
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