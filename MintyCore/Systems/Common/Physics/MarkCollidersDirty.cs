using MintyCore.Components.Common.Physic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Common.Physics;

//TODO change this to set colliders dynamically dirty
/// <summary>
///     System to mark colliders components dirty
/// </summary>
[ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
[ExecuteAfter(typeof(CollisionSystem))]
public partial class MarkCollidersDirty : ASystem
{
    [ComponentQuery] private readonly ComponentQuery<Collider> _componentQuery = new();

    /// <inheritdoc />
    public override Identification Identification => SystemIDs.MarkCollidersDirty;

    /// <inheritdoc />
    protected override void Execute()
    {
        if (World is null) return;
        foreach (var entity in _componentQuery)
        {
            ref var collider = ref entity.GetCollider();
            var bodyRef = World.PhysicsWorld.Simulation.Bodies.GetBodyReference(collider.BodyHandle);

            if (!bodyRef.Exists) continue;

            collider.Dirty = 1;
        }
    }

    /// <inheritdoc />
    public override void Setup(SystemManager systemManager)
    {
        _componentQuery.Setup(this);
    }

}