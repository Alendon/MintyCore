using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BulletSharp;
using BulletSharp.Math;
using MintyCore.Physics.Native;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Physics
{
    /// <summary>
    /// Helper class which contains methods to create various physic objects
    /// </summary>
    public static class PhysicsObjects
    {
        /// <summary>
        /// Create a new <see cref="MotionState"/>
        /// </summary>
        /// <returns><see cref="NativeMotionState"/> which holds a reference to the created <see cref="MotionState"/></returns>
        public static unsafe NativeMotionState CreateMotionState(Matrix transform, Matrix centerOfMassOffset = default)
        {
            if (centerOfMassOffset == default)
            {
                var identityMatrix = System.Numerics.Matrix4x4.Identity;
                centerOfMassOffset = *(Matrix*)&identityMatrix;
            }

            var motionState = new DefaultMotionState(transform, centerOfMassOffset);
            var handle = GCHandle.Alloc(motionState, GCHandleType.Weak);
            motionState.UserPointer = GCHandle.ToIntPtr(handle);
            var disposer = new UnmanagedDisposer(&DisposeMotionState, motionState.Native);

            return new NativeMotionState(motionState.Native, disposer);
        }

        private static void DisposeMotionState(IntPtr motionState)
        {
            if (motionState == IntPtr.Zero) return;
            var handle = GCHandle.FromIntPtr(UnsafeNativeMethods.btDefaultMotionState_getUserPointer(motionState));
            if (handle.Target is DefaultMotionState motionStateObj)
            {
                motionStateObj.Dispose();
                return;
            }

            UnsafeNativeMethods.btMotionState_delete(motionState);
        }

        /// <summary>
        /// Create a new <see cref="GImpactMeshShape"/>. Used for dynamic objects
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape GImpactMeshShape(StridingMeshInterface meshInterface)
        {
            return GetNativeCollisionShape(new GImpactMeshShape(meshInterface));
        }
        
        /// <summary>
        /// Create a new <see cref="CompoundShape"/> with the given children
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateCompoundShape(bool enableDynamicAabb = true, params (Matrix localTransform, CollisionShape shape)[] children)
        {
            var shape = new CompoundShape(enableDynamicAabb, children.Length);
            foreach (var (localTransform, childShape) in children)
            {
                shape.AddChildShape(localTransform, childShape);
            }

            return GetNativeCollisionShape(shape);
        }
        
        /// <summary>
        /// Create a new <see cref="CompoundShape"/> with the given children
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateCompoundShape(bool enableDynamicAabb = true, params (Matrix localTransform, NativeCollisionShape shape)[] children)
        {
            var shape = new CompoundShape(enableDynamicAabb, children.Length);
            foreach (var (localTransform, childShape) in children)
            {
                shape.AddChildShape(localTransform, childShape.GetCollisionShape());
            }

            return GetNativeCollisionShape(shape);
        }

        /// <summary>
        /// Create a new empty <see cref="CompoundShape"/>.
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateCompoundShape(bool enableDynamicAabb = true, int initialSize = 0)
        {
            return GetNativeCollisionShape(new CompoundShape(enableDynamicAabb, initialSize));
        }
        
        /// <summary>
        /// Create a new <see cref="ConvexHullShape"/>.
        /// </summary>
        /// <param name="points">A pointer to an array of points to use</param>
        /// <param name="numPoints">How many points to use</param>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static unsafe NativeCollisionShape CreateConvexHullShape(Vector3* points, int numPoints)
        {
            return GetNativeCollisionShape(new ConvexHullShape(points, numPoints));
        }
        
        /// <summary>
        /// Create a new <see cref="ConvexHullShape"/>.
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateConvexHullShape(IEnumerable<Vector3> points)
        {
            return GetNativeCollisionShape(new ConvexHullShape(points));
        }
        
        /// <summary>
        /// Create a new <see cref="ConvexHullShape"/>.
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateConvexHullShape(IEnumerable<Vector3> points, int count)
        {
            return GetNativeCollisionShape(new ConvexHullShape(points, count));
        }
        
        /// <summary>
        /// Create a new <see cref="ConvexHullShape"/>.
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateConvexHullShape(Vector3[] points, int count)
        {
            return GetNativeCollisionShape(new ConvexHullShape(points, count));
        }
        
        /// <summary>
        /// Create a new <see cref="ConvexHullShape"/>.
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateConvexHullShape(Vector3[] points)
        {
            return GetNativeCollisionShape(new ConvexHullShape(points));
        }
        
        /// <summary>
        /// Create a new <see cref="ScaledBvhTriangleMeshShape"/>. A scaled version of <see cref="BvhTriangleMeshShape"/>
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateScaledBvhTriangleMeshShape(BvhTriangleMeshShape shape, Vector3 scale)
        {
            return GetNativeCollisionShape(new ScaledBvhTriangleMeshShape(shape, scale));
        }

        /// <summary>
        /// Create a new <see cref="BvhTriangleMeshShape"/>. Used for static objects, with a maximum of 2 million triangles
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateBvhTriangleMeshShape(StridingMeshInterface meshInterface,
            bool useQuantizedAabbCompression, bool buildBvh = true)
        {
            return GetNativeCollisionShape(new BvhTriangleMeshShape(meshInterface, useQuantizedAabbCompression,
                buildBvh));
        }

        /// <summary>
        /// Create a new <see cref="StaticPlaneShape"/>. Can only be used in a static context
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateStaticPlaneShape(Vector3 planeNormal, float planeConstant)
        {
            return GetNativeCollisionShape(new StaticPlaneShape(planeNormal, planeConstant));
        }

        /// <summary>
        /// Create a new primitive <see cref="CylinderShape"/>
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateCylinderShape(Vector3 halfExtent)
        {
            return GetNativeCollisionShape(new CylinderShape(halfExtent));
        }

        /// <summary>
        /// Create a new primitive <see cref="SphereShape"/>
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateSphereShape(float radius)
        {
            return GetNativeCollisionShape(new SphereShape(radius));
        }

        /// <summary>
        /// Create a new primitive <see cref="CapsuleShape"/>
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateCapsuleShape(float radius, float height)
        {
            return GetNativeCollisionShape(new CapsuleShape(radius, height));
        }

        /// <summary>
        /// Create a new primitive <see cref="BoxShape"/>
        /// </summary>
        /// <returns><see cref="NativeCollisionShape"/> with a reference to the created shape</returns>
        public static NativeCollisionShape CreateBoxShape(Vector3 halfExtent)
        {
            return GetNativeCollisionShape(new BoxShape(halfExtent));
        }

        private static unsafe NativeCollisionShape GetNativeCollisionShape(CollisionShape shape)
        {
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

        /// <summary>
        /// Create a new <see cref="RigidBody"/> with a prepared <see cref="RigidBodyConstructionInfo"/>. Remember to dispose the construction info, when no longer needed
        /// </summary>
        /// <returns><see cref="NativeCollisionObject"/> with a reference to the created rigid body</returns>
        public static unsafe NativeCollisionObject CreateRigidBody(RigidBodyConstructionInfo constructionInfo)
        {
            var body = new RigidBody(constructionInfo);
            var disposer = new UnmanagedDisposer(&DisposeCollisionObject, body.Native);
            return new NativeCollisionObject(body.Native, disposer);
        }

        /// <summary>
        /// Create a new <see cref="RigidBody"/>
        /// </summary>
        /// <returns><see cref="NativeCollisionObject"/> with a reference to the created rigid body</returns>
        public static unsafe NativeCollisionObject CreateRigidBody(float mass, MotionState motionState,
            CollisionShape collisionShape)
        {
            var constructionInfo = new RigidBodyConstructionInfo(mass, motionState, collisionShape);
            var body = new RigidBody(constructionInfo);
            var disposer = new UnmanagedDisposer(&DisposeCollisionObject, body.Native);
            return new NativeCollisionObject(body.Native, disposer);
        }

        /// <summary>
        /// Create a new <see cref="RigidBody"/>
        /// </summary>
        /// <returns><see cref="NativeCollisionObject"/> with a reference to the created rigid body</returns>
        public static unsafe NativeCollisionObject CreateRigidBody(float mass, NativeMotionState motionState,
            NativeCollisionShape collisionShape)
        {
            var shape = collisionShape.GetCollisionShape();
            var constructionInfo = new RigidBodyConstructionInfo(mass, motionState.GetMotionState(), shape,
                shape.CalculateLocalInertia(mass));
            var body = new RigidBody(constructionInfo);
            constructionInfo.Dispose();
            var disposer = new UnmanagedDisposer(&DisposeCollisionObject, body.Native);
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
    }
}