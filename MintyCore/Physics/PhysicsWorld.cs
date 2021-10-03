using System;
using System.Collections.Generic;
using MintyBulletSharp;
using MintyBulletSharp.Math;
using MintyCore.Physics.Native;

namespace MintyCore.Physics
{
    /// <summary>
    ///     Holds all relevant data and logic to simulate and interact with a physics world
    /// </summary>
    public class PhysicsWorld : IDisposable
    {
        private readonly DbvtBroadphase _broadphase;
        private readonly CollisionConfiguration _collisionConfiguration;
        private readonly CollisionDispatcher _collisionDispatcher;
        private readonly DiscreteDynamicsWorld _world;


        /// <summary>
        ///     Create a new physics world
        /// </summary>
        public PhysicsWorld()
        {
            _collisionConfiguration = new DefaultCollisionConfiguration();
            _collisionDispatcher = new CollisionDispatcher(_collisionConfiguration);
            _broadphase = new DbvtBroadphase();
            _world = new DiscreteDynamicsWorld(_collisionDispatcher, _broadphase, null, _collisionConfiguration);
        }

        /// <summary>
        ///     Removes and deletes all remaining <see cref="CollisionObject" />
        ///     and disposes all data for the physics simulation of this world
        /// </summary>
        public void Dispose()
        {
            List<CollisionObject> toRemove = new(_world.NumCollisionObjects);
            foreach (var collisionObject in _world.CollisionObjectArray) toRemove.Add(collisionObject);

            foreach (var collisionObject in toRemove)
            {
                _world.RemoveCollisionObject(collisionObject);
            }

            _collisionConfiguration.Dispose();
            _collisionDispatcher.Dispose();
            _broadphase.Dispose();
            _world.Dispose();
        }

        /// <summary>
        ///     Calculate physics for a given time
        /// </summary>
        public void StepSimulation(float timeStep)
        {
            _world.StepSimulation(timeStep);
        }

        /// <summary>
        ///     Remove a collision object from the world
        /// </summary>
        public void RemoveCollisionObject(NativeCollisionObject nativeCollisionObject)
        {
            var collisionObject = nativeCollisionObject.GetCollisionObject();
            //As the collision world holds a reference to all managed collision objects,
            //the collision object can not be a part of it if its null
            if (collisionObject is not null) RemoveCollisionObject(collisionObject);
        }

        /// <summary>
        ///     Remove a collision object from the world
        /// </summary>
        public void RemoveCollisionObject(CollisionObject collisionObject)
        {
            _world.RemoveCollisionObject(collisionObject);
        }

        /// <summary>
        ///     Add a collision object to the world
        /// </summary>
        public void AddCollisionObject(NativeCollisionObject nativeCollisionObject)
        {
            var collisionObject = nativeCollisionObject.GetCollisionObject() ??
                                  new CollisionObject(ConstructionInfo.Null)
                                      { Native = nativeCollisionObject.NativePtr };
            AddCollisionObject(collisionObject);
        }
        
        /// <summary>
        /// Perform a raycast. For more information view the bullet documentation
        /// </summary>
        public void PerformRayCast(Vector3 from, Vector3 to, RayResultCallback callback)
        {
            _world.RayTest(from, to, callback );
        }
        
        /// <summary>
        /// Perform contact test. For more information view the bullet documentation
        /// </summary>
        public void PerformContactTest(CollisionObject collisionObject, ContactResultCallback callback)
        {
            _world.ContactTest(collisionObject, callback);
        }

        /// <summary>
        /// Add a <see cref="TypedConstraint"/>. For more information view the bullet documentation
        /// </summary>
        public void AddConstraint(TypedConstraint constraint, bool disablePhysicsBetweenLinkedBodies = false)
        {
            _world.AddConstraint(constraint, disablePhysicsBetweenLinkedBodies);
        }

        /// <summary>
        /// Remove a <see cref="TypedConstraint"/>. For more information view the bullet documentation
        /// </summary>
        public void RemoveConstraint(TypedConstraint constraint)
        {
            _world.RemoveConstraint(constraint);
        }
        
        /// <summary>
        /// Add a <see cref="IAction"/>. For more information view the bullet documentation
        /// </summary>
        public void AddConstraint(IAction action)
        {
            _world.AddAction(action);
        }

        /// <summary>
        /// Remove a <see cref="IAction"/>. For more information view the bullet documentation
        /// </summary>
        public void RemoveConstraint(IAction action)
        {
            _world.RemoveAction(action);
        }

        /// <summary>
        ///     Add a collision object to the world
        /// </summary>
        public void AddCollisionObject(CollisionObject collisionObject)
        {
            _world.AddCollisionObject(collisionObject);
        }
    }
}