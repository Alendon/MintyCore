using MintyCore.Components.Common.Physic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Common.Physics;

[ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
[ExecuteAfter(typeof(CollisionSystem))]
public partial class MarkCollidersDirty : ASystem
{
    [ComponentQuery] private ComponentQuery<Collider> _componentQuery = new();

    public override Identification Identification => SystemIDs.MarkCollidersDirty;

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

    public override void Setup()
    {
        _componentQuery.Setup(this);
    }

    public override void Dispose()
    {
    }
}