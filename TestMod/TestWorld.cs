using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using MintyCore;
using MintyCore.ECS;
using MintyCore.Modding;
using MintyCore.Network;
using MintyCore.Physics;
using MintyCore.Registries;
using MintyCore.Utils;
using Serilog;
using TestMod.Identifications;

namespace TestMod;

[RegisterWorld("test")]
public sealed class TestWorld : IWorld
{
    public TestWorld(IComponentManager componentManager, IArchetypeManager archetypeManager,
        IPlayerHandler playerHandler, INetworkHandler networkHandler, ITestDependency testDependency,
        IModManager modManager, bool isServerWorld)
    {
        IsServerWorld = isServerWorld;
        Log.Debug("This world is a server world: {IsServerWorld}", isServerWorld);
        
        testDependency.DoSomething();

        EntityManager = new EntityManager(this, archetypeManager, playerHandler, networkHandler);
        SystemManager = new SystemManager(this, componentManager, modManager);
        PhysicsWorld = MintyCore.Physics.PhysicsWorld.Create(
            new NarrowPhaseCallbacks(new SpringSettings(30f, 1f), 1f, 2f),
            new PoseIntegratorCallbacks(new Vector3(0, -10, 0), 0.03f, 0.03f),
            new SolveDescription(8, 8));
    }

    public void Dispose()
    {
        SystemManager.Dispose();
        EntityManager.Dispose();
        PhysicsWorld.Dispose();
    }

    public bool IsExecuting { get; private set; }
    public bool IsServerWorld { get; init; }
    public Identification Identification => WorldIDs.Test;
    public SystemManager SystemManager { get; }
    public IEntityManager EntityManager { get; }
    public IPhysicsWorld PhysicsWorld { get; }

    public void Tick()
    {
        IsExecuting = true;
        SystemManager.Execute();
        IsExecuting = false;
    }

    private readonly struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        private readonly SpringSettings _contactSpringiness;
        private readonly float _frictionCoefficient;
        private readonly float _maximumRecoveryVelocity;

        public NarrowPhaseCallbacks(SpringSettings contactSpringiness, float frictionCoefficient,
            float maximumRecoveryVelocity)
        {
            _contactSpringiness = contactSpringiness;
            _frictionCoefficient = frictionCoefficient;
            _maximumRecoveryVelocity = maximumRecoveryVelocity;
        }

        public void Initialize(Simulation simulation)
        {
        }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b,
            ref float speculativeMargin)
        {
            return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
        }

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair,
            ref TManifold manifold,
            out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial.FrictionCoefficient = _frictionCoefficient;
            pairMaterial.MaximumRecoveryVelocity = _maximumRecoveryVelocity;
            pairMaterial.SpringSettings = _contactSpringiness;
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

    private struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        private Vector<float> _dtAngularDamping;

        private Vector3Wide _dtGravity;
        private Vector<float> _dtLinearDamping;
        private readonly float _angularDamping;
        private readonly Vector3 _gravity;
        private readonly float _linearDamping;

        public PoseIntegratorCallbacks(Vector3 gravity, float angularDamping, float linearDamping)
        {
            AngularIntegrationMode = AngularIntegrationMode.Nonconserving;
            IntegrateVelocityForKinematics = false;
            AllowSubstepsForUnconstrainedBodies = false;

            _gravity = gravity;
            _angularDamping = angularDamping;
            _linearDamping = linearDamping;

            _dtGravity = default;
            _dtAngularDamping = default;
            _dtLinearDamping = default;
        }


        public void Initialize(Simulation simulation)
        {
        }

        public void PrepareForIntegration(float dt)
        {
            _dtGravity = Vector3Wide.Broadcast(_gravity * dt);
            _dtLinearDamping = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - _linearDamping, 0, 1), dt));
            _dtAngularDamping = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - _angularDamping, 0, 1), dt));
        }

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
            BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt,
            ref BodyVelocityWide velocity)
        {
            velocity.Linear = (velocity.Linear + _dtGravity) * _dtLinearDamping;
            velocity.Angular *= _dtAngularDamping;
        }

        public AngularIntegrationMode AngularIntegrationMode { get; }
        public bool AllowSubstepsForUnconstrainedBodies { get; }
        public bool IntegrateVelocityForKinematics { get; }
    }
}