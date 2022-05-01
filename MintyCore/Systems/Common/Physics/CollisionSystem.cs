using System.Diagnostics;
using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Physics;
using MintyCore.Registries;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Common.Physics;

/// <summary>
///     System which adds and removes collision object to the <see cref="IWorld.PhysicsWorld" /> and updates the associated
///     <see cref="Entity" />
/// </summary>
[RegisterSystem("collision")]
[ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
public partial class CollisionSystem : ASystem
{
    private readonly Stopwatch _physic = new();

    [ComponentQuery] private readonly CollisionApplyQuery<(Position, Rotation ), Collider> _query = new();

    private double _passedDeltaTime;

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="CollisionSystem" />
    /// </summary>
    public override Identification Identification => SystemIDs.Collision;

    /// <inheritdoc />
    public override void Setup(SystemManager systemManager)
    {
        _query.Setup(this);

        _physic.Start();
        EntityManager.PreEntityDeleteEvent += OnEntityDelete;
    }

    public override void Dispose()
    {
        EntityManager.PreEntityDeleteEvent -= OnEntityDelete;
        base.Dispose();
    }

    /// <summary>
    ///     Checks if the entity has a rigid body in the physics world and removes it
    /// </summary>
    private void OnEntityDelete(IWorld world, Entity entity)
    {
        if (World != world || !ArchetypeManager.HasComponent(entity.ArchetypeId, ComponentIDs.Collider)) return;

        var collider = World.EntityManager.GetComponent<Collider>(entity);

        var bodyRef = World.PhysicsWorld.Simulation.Bodies.GetBodyReference(collider.BodyHandle);
        if (!bodyRef.Exists) return;

        World.PhysicsWorld.Simulation.Bodies.Remove(collider.BodyHandle);
    }

    /// <inheritdoc />
    protected override void Execute()
    {
        if (World is null) return;

        _physic.Stop();
        _passedDeltaTime += _physic.Elapsed.TotalSeconds;
        while (_passedDeltaTime >= PhysicsWorld.FixedDeltaTime)
        {
            World.PhysicsWorld.StepSimulation(PhysicsWorld.FixedDeltaTime /*, _dispatcher*/);
            _passedDeltaTime -= PhysicsWorld.FixedDeltaTime;
        }

        _physic.Restart();

        foreach (var entity in _query)
        {
            var collider = entity.GetCollider();

            var bodyRef = World.PhysicsWorld.Simulation.Bodies.GetBodyReference(collider.BodyHandle);

            if (!bodyRef.Exists) continue;

            ref var rot = ref entity.GetRotation();
            ref var pos = ref entity.GetPosition();

            {
                rot.Value = bodyRef.Pose.Orientation;
                pos.Value = bodyRef.Pose.Position;

                //pos.Dirty = 1;
                //rot.Dirty = 1;
            }
        }
    }
}