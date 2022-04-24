using System;
using System.Diagnostics;
using System.Numerics;
using BepuPhysics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using MintyCore.Registries;

namespace MintyCore.Components.Common.Physic;
//TODO Adjust the synchronization logic to remove stuttering

/// <summary>
///     Holds all relevant collider data
/// </summary>
[RegisterComponent("collider")]
public struct Collider : IComponent
{
    /// <inheritdoc />
    public bool Dirty { get; set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Collider" /> Component
    /// </summary>
    public Identification Identification => ComponentIDs.Collider;

    /// <summary>
    ///     The body handle of the collider to access it in the <see cref="Physics.PhysicsWorld" />
    /// </summary>
    public BodyHandle BodyHandle;

    /// <summary>
    ///     Is the collider added to a physics world
    /// </summary>
    public bool AddedToPhysicsWorld => BodyHandle.Value >= 0;


    /// <inheritdoc />
    public void DecreaseRefCount()
    {
    }

    /// <inheritdoc />
    public void IncreaseRefCount()
    {
    }

    /// <inheritdoc />
    public void PopulateWithDefaultValues()
    {
        BodyHandle.Value = -1;
    }


    /// <inheritdoc />
    public void Serialize(DataWriter writer, IWorld world, Entity entity)
    {
        var bodyRef = world.PhysicsWorld.Simulation.Bodies.GetBodyReference(BodyHandle);
        if (!bodyRef.Exists)
        {
            //Mark that there is no content
            writer.Put(false);
            return;
        }

        //Mark that there is content
        writer.Put(true);

        var orientation = bodyRef.Pose.Orientation;
        var position = bodyRef.Pose.Position;

        writer.Put(orientation);
        writer.Put(position);

        writer.Put(bodyRef.Velocity.Angular);
        writer.Put(bodyRef.Velocity.Linear);
    }


    /// <inheritdoc />
    public bool Deserialize(DataReader reader, IWorld world, Entity entity)
    {
        if (!reader.TryGetBool(out var hasContent) || !hasContent) return false;

        if (!reader.TryGetQuaternion(out var orientation)
            || !reader.TryGetVector3(out var position)
            || !reader.TryGetVector3(out var angularVelocity)
            || !reader.TryGetVector3(out var linearVelocity)) return false;

        var bodyRef = world.PhysicsWorld.Simulation.Bodies.GetBodyReference(BodyHandle);
        if (!bodyRef.Exists /*|| !bodyRef.Awake*/) return true;

        ref var pose = ref bodyRef.Pose;
        ref var velocity = ref bodyRef.Velocity;


        if (!VelocityNearZero(linearVelocity, angularVelocity)
            && VelocityApproximatelyEqual(velocity.Linear, linearVelocity, 3, 3f, 0.2f)
            && PositionApproximatelyEqual(pose.Position, position, velocity.Linear)) return true;


        velocity.Angular = angularVelocity;
        velocity.Linear = linearVelocity;
        pose.Orientation = orientation;
        pose.Position = position;

        return true;
    }

    private static bool VelocityNearZero(Vector3 linearVelocity, Vector3 rotationalVelocity, float nearZero = 0.05f)
    {
        return linearVelocity.Length() < nearZero && rotationalVelocity.Length() < nearZero;
    }

    private static int _tick;
    private static readonly Stopwatch _lastUpdate = Stopwatch.StartNew();
    private static float _timeDelta;

    private static bool PositionApproximatelyEqual(Vector3 oldPos, Vector3 newPos, Vector3 velocity)
    {
        if (_tick != Engine.Tick)
        {
            _lastUpdate.Stop();
            _timeDelta = (float)_lastUpdate.Elapsed.TotalSeconds;
            _lastUpdate.Restart();
            _tick = Engine.Tick;
        }


        var potentialPastPos = oldPos - velocity * _timeDelta;
        var potentialHalfPastPos = oldPos - velocity * _timeDelta / 2;

        var difference = oldPos - potentialHalfPastPos;

        var acceptedDifference = difference.Length() * 10.0f;

        var serverNowDifference = newPos - oldPos;
        var serverHalfDifference = newPos - potentialHalfPastPos;
        var serverPastDifference = newPos - potentialPastPos;

        return serverNowDifference.Length() < acceptedDifference ||
               serverPastDifference.Length() < acceptedDifference ||
               serverHalfDifference.Length() < acceptedDifference;
    }

    private static bool VelocityApproximatelyEqual(Vector3 oldVel, Vector3 newVel, float acceptedNormalDelta,
        float acceptedLengthDelta, float acceptedAbsoluteLengthDelta)
    {
        var oldVelLength = oldVel.Length();
        var newVelLength = newVel.Length();
        var lengthDelta = MathF.Abs(oldVelLength - newVelLength);
        var lengthQuotient = lengthDelta / oldVelLength;

        var oldNormal = Vector3.Normalize(oldVel);
        var newNormal = Vector3.Normalize(newVel);
        var normalDelta = oldNormal - newNormal;
        var normalDeltaLength = normalDelta.Length();

        return (lengthQuotient < acceptedLengthDelta || lengthDelta <= acceptedAbsoluteLengthDelta) &&
               normalDeltaLength < acceptedNormalDelta
               || CheckAbruptNearZero();

        bool CheckAbruptNearZero()
        {
            const float compare = 0.1f;
            return oldVel.X < compare && newVel.X > compare || oldVel.Y < compare && newVel.Y > compare ||
                   oldVel.Z < compare && newVel.Z > compare;
        }
    }
}