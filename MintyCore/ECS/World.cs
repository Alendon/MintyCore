using MintyCore.Identifications;
using MintyCore.Physics;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     The <see cref="World" /> represents a unique simulation
/// </summary>
public class World : IWorld
{
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
        EntityManager = new EntityManager(this);
        SystemManager = new SystemManager(this);
        PhysicsWorld = new PhysicsWorld();
    }

    /// <summary>
    ///     Whether or not the systems are executing now
    /// </summary>
    public bool IsExecuting { get; private set; }

    /// <summary>
    ///     The SystemManager of the <see cref="World" />
    /// </summary>
    public SystemManager SystemManager { get; }

    /// <summary>
    ///     The EntityManager of the <see cref="World" />
    /// </summary>
    public EntityManager EntityManager { get; }

    /// <summary>
    ///     The <see cref="PhysicsWorld" /> of the <see cref="World" />
    /// </summary>
    public PhysicsWorld PhysicsWorld { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        EntityManager.Dispose();
        SystemManager.Dispose();
        PhysicsWorld.Dispose();
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