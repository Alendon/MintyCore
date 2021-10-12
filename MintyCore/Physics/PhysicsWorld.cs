using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using MintyCore.Utils;

namespace MintyCore.Physics
{
    /// <summary>
    ///     Holds all relevant data and logic to simulate and interact with a physics world
    /// </summary>
    public class PhysicsWorld : IDisposable
    {
        /// <summary>
        /// The internal simulation
        /// </summary>
        public readonly Simulation Simulation;

        public const float FixedDeltaTime = 1 / 20f;


        /// <summary>
        ///     Create a new physics world
        /// </summary>
        public PhysicsWorld()
        {
            var narrowPhaseCallback = new MintyNarrowPhaseCallback(_internalNarrowPhaseCallbackCreator());
            var poseIntegratorCallback = new MintyPoseIntegratorCallback(_internalPoseIntegratorCallbackCreator());
            
            Simulation = Simulation.Create(AllocationHandler.BepuBufferPool,
                narrowPhaseCallback,poseIntegratorCallback,
                new PositionLastTimestepper());
        }


        /// <inheritdoc />
        public void Dispose()
        {
            Simulation.Dispose();
        }

        /// <summary>
        ///     Calculate physics for a given time
        /// </summary>
        public void StepSimulation(float timeStep, IThreadDispatcher? dispatcher = null)
        {
            Simulation.Timestep(timeStep, dispatcher);
        }
        
        /// <summary>
        ///     Add a body to the world
        /// </summary>
        public BodyHandle AddBody(BodyDescription bodyDescription)
        {
            return Simulation.Bodies.Add(bodyDescription);
        }

        /// <summary>
        ///     Remove a body from the world
        /// </summary>
        public void RemoveBody(BodyHandle handle)
        {
            Simulation.Bodies.Remove(handle);
        }

        public TypedIndex AddShape<TShape>(TShape shape) where TShape : unmanaged, IShape
        {
            return Simulation.Shapes.Add(shape);
        }

        public void RayCast(Vector3 origin, Vector3 direction, float maximumT )
        {
            HitHandler handler = default;
            Simulation.RayCast(origin, direction, maximumT, ref handler );
            throw new NotImplementedException();
        }
        
        private struct HitHandler : IRayHitHandler
        {
            public float T;
            public Vector3 Normal;
            public CollidableReference Collidable;
            
            public bool AllowTest(CollidableReference collidable)
            {
                return true;
            }

            public bool AllowTest(CollidableReference collidable, int childIndex)
            {
                return true;
            }

            public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable,
                int childIndex)
            {
                if (t < maximumT && t < T)
                {
                    T = t;
                    Normal = normal;
                    Collidable = collidable;
                } 
            }
        }

        private static Func<IPoseIntegratorCallbacks> _internalPoseIntegratorCallbackCreator =
            () => new DefaultPoseIntegratorCallback {Gravity = new Vector3(0,-10,0), AngularDamping = 0.03f, LinearDamping = 0.03f};

        private static Func<INarrowPhaseCallbacks> _internalNarrowPhaseCallbackCreator =
            () => new DefaultNarrowPhaseIntegratorCallback();

        public static AngularIntegrationMode
            PoseIntegratorAngularIntegrationMode = AngularIntegrationMode.Nonconserving;

        public static void SetPoseIntegratorCallback(Func<IPoseIntegratorCallbacks> callbackCreator)
        {
            _internalPoseIntegratorCallbackCreator = callbackCreator;
        }

        public static void ResetPoseIntegratorCallback()
        {
            _internalPoseIntegratorCallbackCreator =
                () => new DefaultPoseIntegratorCallback {Gravity = new Vector3(0,-10,0), AngularDamping = 0.03f, LinearDamping = 0.03f};
        }
        
        public static void SetNarrowPhaseCallback(Func<INarrowPhaseCallbacks> callbackCreator) 
        {
            _internalNarrowPhaseCallbackCreator = callbackCreator;
        }

        public static void ResetNarrowPhaseCallback()
        {
            _internalNarrowPhaseCallbackCreator =
                () => new DefaultNarrowPhaseIntegratorCallback();
        }



    }
    
    class DefaultPoseIntegratorCallback : IPoseIntegratorCallbacks
    {
        public Vector3 Gravity;
        public float LinearDamping;
        public float AngularDamping;
        
        private Vector3 _dtGravity;
        private float _dtLinearDamping;
        private float _dtAngularDamping;
        
        public void Initialize(Simulation simulation)
        {
            
        }

        public void PrepareForIntegration(float dt)
        {
            _dtGravity = Gravity * dt;
            _dtLinearDamping = MathF.Pow(MathHelper.Clamp(1 - LinearDamping, 0, 1), dt);
            _dtAngularDamping = MathF.Pow(MathHelper.Clamp(1 - AngularDamping, 0, 1), dt);
        }

        public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex,
            ref BodyVelocity velocity)
        {
            if(localInertia.InverseMass == 0) return;
            velocity.Linear = (velocity.Linear + _dtGravity) * _dtLinearDamping;
            velocity.Angular = velocity.Angular * _dtAngularDamping;
        }

        public AngularIntegrationMode AngularIntegrationMode { get; }
    }
    
    class DefaultNarrowPhaseIntegratorCallback : INarrowPhaseCallbacks
    {
        public SpringSettings ContactSpringiness;
        public float MaximumRecoveryVelocity;
        public float FrictionCoefficient;
        
        public void Initialize(Simulation simulation)
        {
            if (ContactSpringiness.AngularFrequency != 0 || ContactSpringiness.TwiceDampingRatio != 0) return;
            
            ContactSpringiness = new SpringSettings(30, 1);
            MaximumRecoveryVelocity = 2f;
            FrictionCoefficient = 1f;
        }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
        {
            return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
        }

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
            out PairMaterialProperties pairMaterial) where TManifold : struct, IContactManifold<TManifold>
        {
            pairMaterial.FrictionCoefficient = FrictionCoefficient;
            pairMaterial.MaximumRecoveryVelocity = MaximumRecoveryVelocity;
            pairMaterial.SpringSettings = ContactSpringiness;
            return true;
        }

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return AllowContactGeneration(workerIndex, pair.A, pair.B);
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
            ref ConvexContactManifold manifold)
        {
            return true;
        }

        public void Dispose()
        {
        }
    }

    readonly struct MintyPoseIntegratorCallback : IPoseIntegratorCallbacks
    {
        private readonly IPoseIntegratorCallbacks _internalCallback;

        public MintyPoseIntegratorCallback(IPoseIntegratorCallbacks internalCallback)
        {
            _internalCallback = internalCallback;
            AngularIntegrationMode = PhysicsWorld.PoseIntegratorAngularIntegrationMode;
        }


        public void Initialize(Simulation simulation)
        {
            _internalCallback.Initialize(simulation);
        }

        public void PrepareForIntegration(float dt)
        {
            _internalCallback.PrepareForIntegration(dt);
        }

        public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex,
            ref BodyVelocity velocity)
        {
            _internalCallback.IntegrateVelocity(bodyIndex, pose, localInertia, workerIndex, ref velocity);
        }

        public AngularIntegrationMode AngularIntegrationMode { get; }
    }


    readonly struct MintyNarrowPhaseCallback : INarrowPhaseCallbacks
    {
        private readonly INarrowPhaseCallbacks _internalCallback;

        public MintyNarrowPhaseCallback(INarrowPhaseCallbacks internalCallback)
        {
            _internalCallback = internalCallback;
        }

        public void Initialize(Simulation simulation)
        {
            _internalCallback.Initialize(simulation);
        }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
        {
            return _internalCallback.AllowContactGeneration(workerIndex, a, b);
        }

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
            out PairMaterialProperties pairMaterial) where TManifold : struct, IContactManifold<TManifold>
        {
            return _internalCallback.ConfigureContactManifold(workerIndex, pair, ref manifold, out pairMaterial);
        }

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return _internalCallback.AllowContactGeneration(workerIndex, pair, childIndexA, childIndexB);
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
            ref ConvexContactManifold manifold)
        {
            return _internalCallback.ConfigureContactManifold(workerIndex, pair, childIndexA, childIndexB,
                ref manifold);
        }

        public void Dispose()
        {
            _internalCallback.Dispose();
        }
    }

    
}