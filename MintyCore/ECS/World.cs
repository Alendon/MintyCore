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
        _physicsWorld = new PhysicsWorld();
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
    public EntityManager EntityManager => _entityManager?? throw new Exception("Object is Disposed");

    /// <summary>
    ///     The <see cref="PhysicsWorld" /> of the <see cref="World" />
    /// </summary>
    public PhysicsWorld PhysicsWorld => _physicsWorld?? throw new Exception("Object is Disposed");

    /// <inheritdoc />
    public void Dispose()
    {
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