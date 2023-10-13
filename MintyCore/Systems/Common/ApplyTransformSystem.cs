using System.Numerics;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Common;

/// <summary>
/// System which calculates the transform matrix of an entity.
/// </summary>
[RegisterSystem("apply_transform")]
[ExecuteInSystemGroup<FinalizationSystemGroup>]
public partial class ApplyTransformSystem : ASystem
{
    [ComponentQuery] private readonly ComponentQuery<Transform, (Position, Rotation, Scale)> _componentQuery = new();
    
    ///<inheritdoc/>
    public override Identification Identification => SystemIDs.ApplyTransform;

    ///<inheritdoc/>
    protected sealed override void Execute()
    {
        foreach (var entity in _componentQuery)
        {
            var position = entity.GetPosition();
            var rotation = entity.GetRotation();
            var scale = entity.GetScale();

            if (!scale.Dirty && !rotation.Dirty && !position.Dirty)
                continue;

            ref var transform = ref entity.GetTransform();
            var value = Matrix4x4.CreateFromQuaternion(rotation.Value) * Matrix4x4.CreateScale(scale.Value) *
                        Matrix4x4.CreateTranslation(position.Value);
            transform.Dirty = true;
            transform.Value = value;
        }
    }

    ///<inheritdoc/>
    public override void Setup(SystemManager systemManager)
    {
        _componentQuery.Setup(this);
    }
}