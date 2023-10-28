using System;
using MintyCore.Physics;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
/// Base interface for world implementations
/// </summary>
public interface IWorld : IDisposable
{
    /// <summary>
    ///     Whether or not the systems are executing now
    /// </summary>
    bool IsExecuting { get; }

    /// <summary>
    ///     Is the world a server world
    /// </summary>
    bool IsServerWorld { get; init; }

    /// <summary>
    /// The Id of the world
    /// </summary>
    Identification Identification { get; }

    //TODO change SystemManager, EntityManager and PhysicsWorld to implemented interfaces
    
    /// <summary>
    ///     The SystemManager of the <see cref="IWorld" />
    /// </summary>
    SystemManager SystemManager { get; }

    /// <summary>
    ///     The EntityManager of the <see cref="IWorld" />
    /// </summary>
    IEntityManager EntityManager { get; }

    /// <summary>
    ///     The <see cref="IPhysicsWorld" /> of the <see cref="IWorld" />
    /// </summary>
    IPhysicsWorld PhysicsWorld { get; }

    /// <summary>
    ///     Simulate one <see cref="IWorld" /> tick
    /// </summary>
    void Tick();
}