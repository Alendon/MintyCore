using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using JetBrains.Annotations;

namespace MintyCore.Physics;

/// <summary>
///    Interface for a physics world
/// </summary>
[PublicAPI]
public interface IPhysicsWorld : IDisposable
{
    /// <summary>
    ///     The internal simulation
    /// </summary>
    Simulation Simulation { get; }

    /// <summary>
    ///     Calculate physics for a given time
    /// </summary>
    void StepSimulation(IThreadDispatcher? dispatcher = null);

    /// <summary>
    ///     Add a body to the world
    /// </summary>
    BodyHandle AddBody(BodyDescription bodyDescription);

    /// <summary>
    ///     Remove a body from the world
    /// </summary>
    void RemoveBody(BodyHandle handle);

    /// <summary>
    ///     Add a shape to the simulation
    /// </summary>
    /// <param name="shape">Shape to add</param>
    /// <typeparam name="TShape">Type of the shape, needs to be unmanaged and <see cref="IShape" /></typeparam>
    /// <returns>Type index of the shape for future access</returns>
    TypedIndex AddShape<TShape>(TShape shape) where TShape : unmanaged, IShape;

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
    bool RayCast(Vector3 origin, Vector3 direction, float maximumT, out float tResult,
        out CollidableReference result, out Vector3 normalResult);
    
    /// <summary>
    ///     The "fixed" delta time for physics simulation
    ///     Don't mess with this
    /// </summary>
    public float FixedDeltaTime { get; set; }
}