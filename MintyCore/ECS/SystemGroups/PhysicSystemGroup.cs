using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.ECS.SystemGroups;

/// <summary>
///     System group for physics
/// </summary>
[RegisterSystem("physic_group")]
[ExecuteInSystemGroup<InitializationSystemGroup>]
public class PhysicSystemGroup(IGameTimer timer) : ASystemGroup
{
    private float _accumulatedDeltaTime;

    /// <inheritdoc />
    public override Identification Identification => SystemIDs.PhysicGroup;
    
    public const float FixedDeltaTime = 1f / 60f;

    /// <inheritdoc />
    public override Task QueueSystem(IEnumerable<Task> dependency)
    {
        List<Task> systemTasks = new();
        _accumulatedDeltaTime += timer.DeltaTime;
        var enumerable = dependency as Task[] ?? dependency.ToArray();
        while (_accumulatedDeltaTime >= FixedDeltaTime)
        {
            systemTasks.Add(base.QueueSystem(enumerable));
            _accumulatedDeltaTime -= FixedDeltaTime;
        }

        return Task.WhenAll(systemTasks);
    }
}