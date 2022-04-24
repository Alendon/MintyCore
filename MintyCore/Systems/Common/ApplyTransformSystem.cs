using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using MintyCore.Registries;

namespace MintyCore.Systems.Common;

[RegisterSystem("apply_transform")]
[ExecuteInSystemGroup(typeof(FinalizationSystemGroup))]
partial class ApplyTransformSystem : ASystem
{
    [ComponentQuery]
    private readonly ComponentQuery<Transform, (Position, Rotation, Scale)> _componentQuery = new();

    public override Identification Identification => SystemIDs.ApplyTransform;

    protected sealed override void Execute()
    {
        foreach (var entity in _componentQuery)
        {
            var position = entity.GetPosition();
            var rotation = entity.GetRotation();
            var scale = entity.GetScale();
            ref var transform = ref entity.GetTransform();
            var value = Matrix4x4.CreateFromQuaternion(rotation.Value) * Matrix4x4.CreateTranslation(position.Value) *
                        Matrix4x4.CreateScale(scale.Value);
            transform.Dirty = true;
            transform.Value = value;
        }
    }

    public override Task QueueSystem(IEnumerable<Task> dependencies)
    {
        return Task.WhenAll(dependencies).ContinueWith(_ => Parallel.ForEach(_componentQuery, Execute));
    }
    
    private void Execute(ComponentQuery<Transform, (Position, Rotation, Scale)>.CurrentEntity entity)
    {
        var position = entity.GetPosition();
        var rotation = entity.GetRotation();
        var scale = entity.GetScale();
        ref var transform = ref entity.GetTransform();
        var value = Matrix4x4.CreateFromQuaternion(rotation.Value) * Matrix4x4.CreateTranslation(position.Value) *
                    Matrix4x4.CreateScale(scale.Value);
        transform.Dirty = true;
        transform.Value = value;
    }


    public override void Setup(SystemManager systemManager)
    {
        _componentQuery.Setup(this);
    }
}