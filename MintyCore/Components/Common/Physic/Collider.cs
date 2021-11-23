using System;
using System.Diagnostics;
using System.Numerics;
using BepuPhysics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common.Physic
{
    /// <summary>
    ///     Holds all relevant collider data
    /// </summary>
    public struct Collider : IComponent
    {
        /// <inheritdoc />
        public byte Dirty { get; set; }

        /// <summary>
        ///     <see cref="Identification" /> of the <see cref="Collider" /> Component
        /// </summary>
        public Identification Identification => ComponentIDs.Collider;

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
        public void Serialize(DataWriter writer, World world, Entity entity)
        {
            var bodyRef = world.PhysicsWorld.Simulation.Bodies.GetBodyReference(BodyHandle);
            if (!bodyRef.Exists)
            {
                //Mark that there is no content
                writer.Put((byte)0);
                return;
            }

            //Mark that there is content
            writer.Put((byte)1);

            var orientation = bodyRef.Pose.Orientation;
            var position = bodyRef.Pose.Position;

            writer.Put(orientation);
            writer.Put(position);

            writer.Put(bodyRef.Velocity.Angular);
            writer.Put(bodyRef.Velocity.Linear);
        }

        private const float ToleratedPositionDelta = 1e2f;
        private const float ToleratedVelocityDelta = 1e-5f;

        private const float ToleratedVelocityDirectionDelta = 1;
        private const float ToleratedVelocityPowerDelta = 0.8f;

        
        /// <inheritdoc />
        public void Deserialize(DataReader reader, World world, Entity entity)
        {
            var hasContent = reader.GetByte();
            if (hasContent == 0) return;

            var orientation = reader.GetQuaternion();
            var position = reader.GetVector3();

            var angularVelocity = reader.GetVector3();
            var linearVelocity = reader.GetVector3();

            var bodyRef = world.PhysicsWorld.Simulation.Bodies.GetBodyReference(BodyHandle);
            if (!bodyRef.Exists /*|| !bodyRef.Awake*/) return;

            ref var pose = ref bodyRef.Pose;
            ref var velocity = ref bodyRef.Velocity;

            
            if (!VelocityNearZero(linearVelocity, angularVelocity)
                && VelocityApproximatelyEqual(velocity.Linear, linearVelocity, 3, 3f, 0.2f)
                /*&& VelocityApproximatelyEqual(velocity.Angular, angularVelocity, 100, 100.2f)*/
                && PositionApproximatelyEqual(pose.Position, position, velocity.Linear, 0.3f)
                /* && RotationApproximatelyEqual(pose.Orientation, orientation, velocity.Angular, 0.2f)*/) return;
            
            
            velocity.Angular = angularVelocity;
            velocity.Linear = linearVelocity;
            pose.Orientation = orientation;
            pose.Position = position;

        }

        private static bool VelocityNearZero(Vector3 linearVelocity, Vector3 rotationalVelocity, float nearZero =0.05f)
        {
            if (linearVelocity.Length() < nearZero && rotationalVelocity.Length() < nearZero) return true;
            return false;
        }

        private static bool RotationApproximatelyEqual(Quaternion oldRot, Quaternion newRot, Vector3 velocity, float acceptedDelta)
        {
            if (velocity.Length() > 1) return true;
            return true;
        }

        private static int _tick = 0;
        private static Stopwatch _lastUpdate = Stopwatch.StartNew();
        private static float _timeDelta = 0;
        
        private static bool PositionApproximatelyEqual(Vector3 oldPos, Vector3 newPos, Vector3 velocity,
            float acceptedDelta)
        {
            if (_tick != Engine.Tick)
            {
                _lastUpdate.Stop();
                _timeDelta = (float)_lastUpdate.Elapsed.TotalSeconds;
                _lastUpdate.Restart();
                _tick = Engine.Tick;
            }
            
            
            var potentialPastPos = oldPos - velocity * _timeDelta;
            var potentialHalfPastPos = oldPos - (velocity * _timeDelta / 2);

            var difference = oldPos - potentialHalfPastPos;
            
            var acceptedDifference = difference.Length() * 10.0f;

            var serverNowDifference = newPos - oldPos;
            var serverHalfDifference = newPos - potentialHalfPastPos;
            var serverPastDifference = newPos - potentialPastPos;

            return serverNowDifference.Length() < acceptedDifference ||
                   serverPastDifference.Length() < acceptedDifference ||
                   serverHalfDifference.Length() < acceptedDifference;
        }

        private static bool VelocityApproximatelyEqual(Vector3 oldVel, Vector3 newVel, float acceptedNormalDelta, float acceptedLengthDelta, float acceptedAbsoluteLengthDelta)
        {
            var oldVelLength = oldVel.Length();
            var newVelLength = newVel.Length();
            var lengthDelta = MathF.Abs(oldVelLength - newVelLength);
            var lengthQuotient = lengthDelta / oldVelLength;

            var oldNormal = Vector3.Normalize(oldVel);
            var newNormal = Vector3.Normalize(newVel);
            var normalDelta = oldNormal - newNormal;
            var normalDeltaLength = normalDelta.Length();

            if ((lengthQuotient < acceptedLengthDelta || lengthDelta <=acceptedAbsoluteLengthDelta ) && normalDeltaLength < acceptedNormalDelta
            || CheckAbruptNearZero())
            {
                return true;
            }
            if (oldVel.Length() <= 1e-10)
            {
                return false;
            }
            return false;

            bool CheckAbruptNearZero()
            {
                const float compare = 0.1f;
                return oldVel.X < compare && newVel.X > compare || oldVel.Y < compare && newVel.Y > compare ||
                       oldVel.Z < compare && newVel.Z > compare;
            }

        }
    }
}