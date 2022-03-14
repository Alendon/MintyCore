using System.Numerics;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Common;

[ExecuteInSystemGroup(typeof(FinalizationSystemGroup))]
[ParallelSystem]
internal partial class ApplyTransformSystem : ASystem
{
    [ComponentQuery]
    private readonly TestComponentQuery<Transform, (Position, Rotation, Scale)> _componentQuery = new();

    public override Identification Identification => SystemIDs.ApplyTransform;

    public override void Dispose()
    {
    }

    protected override void Execute()
    {
        foreach (var entity in _componentQuery)
        {
            var position = entity.GetPosition();
            var rotation = entity.GetRotation();
            var scale = entity.GetScale();
            ref var transform = ref entity.GetTransform();

            var value = Matrix4x4.CreateFromQuaternion(rotation.Value) *
                        Matrix4x4.CreateTranslation(position.Value) *
                        Matrix4x4.CreateScale(scale.Value);

            transform.Dirty = 1;
            transform.Value = value;
        }
    }

    private void Execute(TestComponentQuery<Transform, (Position, Rotation, Scale)>.CurrentEntity entity)
    {
        var position = entity.GetPosition();
        var rotation = entity.GetRotation();
        var scale = entity.GetScale();
        ref var transform = ref entity.GetTransform();

        if (scale.Value.X < 1 && !World!.IsServerWorld)
        {
        }

        var value = Matrix4x4.CreateFromQuaternion(rotation.Value) * Matrix4x4.CreateTranslation(position.Value) *
                    Matrix4x4.CreateScale(scale.Value);

        transform.Dirty = 1;
        transform.Value = value;
    }


    public override void Setup(SystemManager systemManager)
    {
        _componentQuery.Setup(this);
    }
}