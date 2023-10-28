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
[ExecuteInSystemGroup<PhysicSystemGroup>]
public sealed partial class CollisionSystem : ASystem
{
    [ComponentQuery] private readonly CollisionApplyQuery<(Position, Rotation ), Collider> _query = new();

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="CollisionSystem" />
    /// </summary>
    public override Identification Identification => SystemIDs.Collision;

    /// <inheritdoc />
    public override void Setup(SystemManager systemManager)
    {
        _query.Setup(this);

        IEntityManager.AddOnDestroyCallback(OnEntityDelete);
    }

    ///<inheritdoc/>
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        IEntityManager.RemoveOnDestroyCallback(OnEntityDelete);
    }

    /// <summary>
    ///     Checks if the entity has a rigid body in the physics world and removes it
    /// </summary>
    private void OnEntityDelete(IWorld world, Entity entity)
    {
        if (World != world) return;

        var collider = World.EntityManager.TryGetComponent<Collider>(entity, out var hasComponent);

        if (!hasComponent) return;

        var bodyRef = World.PhysicsWorld.Simulation.Bodies.GetBodyReference(collider.Handle);
        if (!bodyRef.Exists) return;

        World.PhysicsWorld.Simulation.Bodies.Remove(collider.Handle);
    }

    /// <inheritdoc />
    protected override void Execute()
    {
        if (World is null) return;

        World.PhysicsWorld.StepSimulation(PhysicsWorld.FixedDeltaTime);

        foreach (var entity in _query)
        {
            var collider = entity.GetCollider();

            var bodyRef = World.PhysicsWorld.Simulation.Bodies.GetBodyReference(collider.Handle);

            if (!bodyRef.Exists) continue;

            ref var rot = ref entity.GetRotation();
            ref var pos = ref entity.GetPosition();

            rot.Dirty = rot.Dirty || rot.Value != bodyRef.Pose.Orientation;
            pos.Dirty = pos.Dirty || pos.Value != bodyRef.Pose.Position;

            rot.Value = bodyRef.Pose.Orientation;
            pos.Value = bodyRef.Pose.Position;
        }
    }
}