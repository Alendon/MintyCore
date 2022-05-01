using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Constraints;
using MintyCore.Identifications;
using MintyCore.Physics;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     The <see cref="World" /> represents a unique simulation
/// </summary>
public class World : IWorld
{
    private SystemManager? _systemManager;
    private EntityManager? _entityManager;
    private PhysicsWorld? _physicsWorld;

    /// <summary>
    ///     Whether or not this world is a server world.
    /// </summary>
    public bool IsServerWorld { get; }

    /// <inheritdoc />
    public Identification Identification => WorldIDs.Default;

    /// <summary>
    ///     Create a new World
    /// </summary>
    public World(bool isServerWorld)
    {
        IsServerWorld = isServerWorld;
        _entityManager = new EntityManager(this);
        _systemManager = new SystemManager(this);
        _physicsWorld = PhysicsWorld.Create(new MintyNarrowPhaseCallback(new SpringSettings(30f, 1f), 1f, 2f),
            new MintyPoseIntegratorCallback(new Vector3(0, -10, 0), 0.03f, 0.03f), new SolveDescription(16),
            new SubsteppingTimestepper());
    }

    /// <summary>
    /// Create a new World
    /// </summary>
    /// <param name="isServerWorld">Whether or not this world is a server world.</param>
    /// <param name="physicsWorld">The physics world to use.</param>
    public World(bool isServerWorld, PhysicsWorld physicsWorld)
    {
        IsServerWorld = isServerWorld;
        _entityManager = new EntityManager(this);
        _systemManager = new SystemManager(this);
        _physicsWorld = physicsWorld;
    }

    /// <summary>
    ///     Whether or not the systems are executing now
    /// </summary>
    public bool IsExecuting { get; private set; }

    /// <summary>
    ///     The SystemManager of the <see cref="World" />
    /// </summary>
    public SystemManager SystemManager => _systemManager ?? throw new Exception("Object is Disposed");

    /// <summary>
    ///     The EntityManager of the <see cref="World" />
    /// </summary>
    public EntityManager EntityManager => _entityManager ?? throw new Exception("Object is Disposed");

    /// <summary>
    ///     The <see cref="PhysicsWorld" /> of the <see cref="World" />
    /// </summary>
    public PhysicsWorld PhysicsWorld => _physicsWorld ?? throw new Exception("Object is Disposed");

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        EntityManager.Dispose();
        SystemManager.Dispose();
        PhysicsWorld.Dispose();

        _entityManager = null;
        _physicsWorld = null;
        _systemManager = null;
    }

    /// <summary>
    ///     Simulate one <see cref="World" /> tick
    /// </summary>
    public void Tick()
    {
        IsExecuting = true;
        SystemManager.Execute();
        IsExecuting = false;
    }
}