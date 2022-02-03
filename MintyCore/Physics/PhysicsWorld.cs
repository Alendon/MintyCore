using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using MintyCore.Utils;

namespace MintyCore.Physics;

/// <summary>
///     Holds all relevant data and logic to simulate and interact with a physics world
/// </summary>
public class PhysicsWorld : IDisposable
{
    /// <summary>
    /// The fixed delta time for physics simulation
    /// </summary>
    public const float FixedDeltaTime = 1 / 20f;

    private static Func<IPoseIntegratorCallbacks> _internalPoseIntegratorCallbackCreator =
        () => new DefaultPoseIntegratorCallback
            { Gravity = new Vector3(0, -10, 0), AngularDamping = 0.03f, LinearDamping = 0.03f };

    private static Func<INarrowPhaseCallbacks> _internalNarrowPhaseCallbackCreator =
        () => new DefaultNarrowPhaseIntegratorCallback();

    /// <summary>
    /// 
    /// </summary>
    public static AngularIntegrationMode
        PoseIntegratorAngularIntegrationMode = AngularIntegrationMode.Nonconserving;

    /// <summary>
    /// The internal simulation
    /// </summary>
    public readonly Simulation Simulation;


    /// <summary>
    ///     Create a new physics world
    /// </summary>
    public PhysicsWorld()
    {
        var narrowPhaseCallback = new MintyNarrowPhaseCallback(_internalNarrowPhaseCallbackCreator());
        var poseIntegratorCallback = new MintyPoseIntegratorCallback(_internalPoseIntegratorCallbackCreator());

        Simulation = Simulation.Create(AllocationHandler.BepuBufferPool,
            narrowPhaseCallback, poseIntegratorCallback, new SolveDescription(4),
            new SubsteppingTimestepper());
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

    /// <summary>
    /// Add a shape to the simulation
    /// </summary>
    /// <param name="shape">Shape to add</param>
    /// <typeparam name="TShape">Type of the shape, needs to be unmanaged and <see cref="IShape"/></typeparam>
    /// <returns>Type index of the shape for future access</returns>
    public TypedIndex AddShape<TShape>(TShape shape) where TShape : unmanaged, IShape
    {
        return Simulation.Shapes.Add(shape);
    }
    
    /// <summary>
    /// Perform a simple raycast
    /// </summary>
    /// <param name="origin">The origin of the ray</param>
    /// <param name="direction">The direction of the ray</param>
    /// <param name="maximumT">The maximum 't' of the ray (probably distance)</param>
    /// <param name="tResult">The 't' of the result (probably distance)</param>
    /// <param name="result">The result collidable</param>
    /// <param name="normalResult">The normal vector of the hit</param>
    /// <returns>Whether or not the ray hit a collidable</returns>
    public bool RayCast(Vector3 origin, Vector3 direction, float maximumT, out float tResult, out CollidableReference result, out Vector3 normalResult)
    {
        HitHandler handler = default;
        Simulation.RayCast(origin, direction, maximumT, ref handler);

        tResult = handler.T;
        result = handler.Collidable;
        normalResult = handler.Normal;
        return handler.HasHit;
    }

    /// <summary>
    /// Set a custom pose integrator callback
    /// </summary>
    public static void SetPoseIntegratorCallback(Func<IPoseIntegratorCallbacks> callbackCreator)
    {
        _internalPoseIntegratorCallbackCreator = callbackCreator;
    }

    /// <summary>
    /// Reset the pose integrator callback to default
    /// </summary>
    public static void ResetPoseIntegratorCallback()
    {
        _internalPoseIntegratorCallbackCreator =
            () => new DefaultPoseIntegratorCallback
                { Gravity = new Vector3(0, -10, 0), AngularDamping = 0.03f, LinearDamping = 0.03f };
    }

    /// <summary>
    /// Set a custom narrow phase callback
    /// </summary>
    public static void SetNarrowPhaseCallback(Func<INarrowPhaseCallbacks> callbackCreator)
    {
        _internalNarrowPhaseCallbackCreator = callbackCreator;
    }

    /// <summary>
    /// Reset the narrow phase callback to default
    /// </summary>
    public static void ResetNarrowPhaseCallback()
    {
        _internalNarrowPhaseCallbackCreator =
            () => new DefaultNarrowPhaseIntegratorCallback();
    }

    private struct HitHandler : IRayHitHandler
    {
        public float T;
        public Vector3 Normal;
        public CollidableReference Collidable;
        public bool HasHit;

        public bool AllowTest(CollidableReference collidable)
        {
            return true;
        }

        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }

        // ReSharper disable once RedundantAssignment
        public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal,
            CollidableReference collidable,
            int childIndex)
        {
            maximumT = t;
            Collidable = collidable;
            T = t;
            Normal = normal;
            HasHit = true;
        }
    }
}

[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
class DefaultPoseIntegratorCallback : IPoseIntegratorCallbacks
{
    private Vector<float> _dtAngularDamping;

    private Vector3Wide _dtGravity;
    private Vector<float> _dtLinearDamping;
    public float AngularDamping;
    public Vector3 Gravity;
    public float LinearDamping;

    public void Initialize(Simulation simulation)
    {
    }

    public void PrepareForIntegration(float dt)
    {
        _dtGravity = Vector3Wide.Broadcast( Gravity * dt);
        _dtLinearDamping = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - LinearDamping, 0, 1), dt));
        _dtAngularDamping = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - AngularDamping, 0, 1), dt));
    }

    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
        BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
        if (localInertia.InverseMass == Vector<float>.Zero) return;
        velocity.Linear = (velocity.Linear + _dtGravity) * _dtLinearDamping;
        velocity.Angular *= _dtAngularDamping;
    }

    public AngularIntegrationMode AngularIntegrationMode { get; }
    public bool AllowSubstepsForUnconstrainedBodies { get; }
    public bool IntegrateVelocityForKinematics { get; }
}

class DefaultNarrowPhaseIntegratorCallback : INarrowPhaseCallbacks
{
    public SpringSettings ContactSpringiness;
    public float FrictionCoefficient;
    public float MaximumRecoveryVelocity;

    public void Initialize(Simulation simulation)
    {
        if (ContactSpringiness.AngularFrequency != 0 || ContactSpringiness.TwiceDampingRatio != 0) return;

        ContactSpringiness = new SpringSettings(30, 1);
        MaximumRecoveryVelocity = 2f;
        FrictionCoefficient = 1f;
    }

    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
    {
        return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
    }

    bool INarrowPhaseCallbacks.ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties pairMaterial)
    {
        return ConfigureContactManifold(workerIndex, pair, ref manifold, out pairMaterial);
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
        return true;
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
        IntegrateVelocityForKinematics = false;
        AllowSubstepsForUnconstrainedBodies = false;
    }


    public void Initialize(Simulation simulation)
    {
        _internalCallback.Initialize(simulation);
    }

    public void PrepareForIntegration(float dt)
    {
        _internalCallback.PrepareForIntegration(dt);
    }

    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
        BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
        _internalCallback.IntegrateVelocity(bodyIndices, position, orientation, localInertia, integrationMask, workerIndex, dt, ref velocity);
    }

    public AngularIntegrationMode AngularIntegrationMode { get; }
    public bool AllowSubstepsForUnconstrainedBodies { get; }
    public bool IntegrateVelocityForKinematics { get; }
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

    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
    {
        return _internalCallback.AllowContactGeneration(workerIndex, a, b, ref speculativeMargin);
    }

    bool INarrowPhaseCallbacks.ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties pairMaterial)
    {
        return ConfigureContactManifold(workerIndex, pair, ref manifold, out pairMaterial);
    }

    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
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