using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.SystemGroups;

/// <summary>
///     System group for physics
/// </summary>
[RegisterSystem("physic_group")]
[ExecuteInSystemGroup(typeof(InitializationSystemGroup))]
public class PhysicSystemGroup : ASystemGroup
{
    private float _accumulatedDeltaTime;

    /// <inheritdoc />
    public override Identification Identification => SystemIDs.PhysicGroup;

    /// <inheritdoc />
    public override Task QueueSystem(IEnumerable<Task> dependency)
    {
        List<Task> systemTasks = new();
        _accumulatedDeltaTime += Engine.DeltaTimeF;
        var enumerable = dependency as Task[] ?? dependency.ToArray();
        while (_accumulatedDeltaTime >= Engine.FixedDeltaTime)
        {
            systemTasks.Add(base.QueueSystem(enumerable));
            _accumulatedDeltaTime -= Engine.FixedDeltaTime;
        }

        return Task.WhenAll(systemTasks);
    }
}