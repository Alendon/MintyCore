using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;

namespace MintyCore.Physics;

/// <summary>
///     Holds all relevant data and logic to simulate and interact with a physics world
/// </summary>
public sealed class PhysicsWorld : IPhysicsWorld
{
    /// <summary>
    ///     The fixed delta time for physics simulation
    /// </summary>
    public const float FixedDeltaTime = 1 / 20f;

    /// <summary>
    ///     The internal simulation
    /// </summary>
    public Simulation Simulation { get; }

    private readonly BufferPool? _bufferPool;
    private bool _disposeBuffer;


    /// <summary>
    /// Create a new physics world
    /// </summary>
    /// <param name="narrowPhaseCallbacks"></param>
    /// <param name="poseIntegratorCallbacks"></param>
    /// <param name="solveDescription"></param>
    /// <param name="timestepper"></param>
    /// <param name="pool">Buffer pool to use in the PhysicsWorld. Should be unique for each world</param>
    /// <param name="initialAllocationSize"></param>
    /// <typeparam name="TNarrowPhaseCallbacks"></typeparam>
    /// <typeparam name="TPoseIntegratorCallbacks"></typeparam>
    /// <returns>Created PhysicsWorld</returns>
    public static PhysicsWorld Create<TNarrowPhaseCallbacks, TPoseIntegratorCallbacks>(
        TNarrowPhaseCallbacks narrowPhaseCallbacks, TPoseIntegratorCallbacks poseIntegratorCallbacks,
        SolveDescription solveDescription, ITimestepper? timestepper = null, BufferPool? pool = null,
        SimulationAllocationSizes? initialAllocationSize = null)
        where TNarrowPhaseCallbacks : struct, INarrowPhaseCallbacks
        where TPoseIntegratorCallbacks : struct, IPoseIntegratorCallbacks
    {
        var poolProvided = pool is not null;
        if (!poolProvided) pool = new BufferPool();

        var simulation = Simulation.Create(pool, narrowPhaseCallbacks, poseIntegratorCallbacks,
            solveDescription, timestepper, initialAllocationSize);
        var physicsWorld = new PhysicsWorld(simulation, pool!)
        {
            _disposeBuffer = !poolProvided
        };
        return physicsWorld;
    }

    private PhysicsWorld(Simulation simulation, BufferPool bufferPool)
    {
        Simulation = simulation;
        _bufferPool = bufferPool;
    }


    /// <inheritdoc />
    public void Dispose()
    {
        Simulation.Dispose();
        if (_disposeBuffer) _bufferPool?.Clear();
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
    ///     Add a shape to the simulation
    /// </summary>
    /// <param name="shape">Shape to add</param>
    /// <typeparam name="TShape">Type of the shape, needs to be unmanaged and <see cref="IShape" /></typeparam>
    /// <returns>Type index of the shape for future access</returns>
    public TypedIndex AddShape<TShape>(TShape shape) where TShape : unmanaged, IShape
    {
        return Simulation.Shapes.Add(shape);
    }

    /// <summary>
    ///     Perform a simple raycast
    /// </summary>
    /// <param name="origin">The origin of the ray</param>
    /// <param name="direction">The direction of the ray</param>
    /// <param name="maximumT">The maximum 't' of the ray (probably distance)</param>
    /// <param name="tResult">The 't' of the result (probably distance)</param>
    /// <param name="result">The result collidable</param>
    /// <param name="normalResult">The normal vector of the hit</param>
    /// <returns>Whether or not the ray hit a collidable</returns>
    public bool RayCast(Vector3 origin, Vector3 direction, float maximumT, out float tResult,
        out CollidableReference result, out Vector3 normalResult)
    {
        HitHandler handler = default;
        Simulation.RayCast(origin, direction, maximumT, ref handler);

        tResult = handler.T;
        result = handler.Collidable;
        normalResult = handler.Normal;
        return handler.HasHit;
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

