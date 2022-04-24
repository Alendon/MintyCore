using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using MintyCore.Registries;

namespace MintyCore.SystemGroups;

/// <summary>
///     Root system group for finalization
/// </summary>
[RegisterSystem("finalization_group")]
[RootSystemGroup]
[ExecuteAfter(typeof(SimulationSystemGroup))]
public class FinalizationSystemGroup : ASystemGroup
{
    /// <inheritdoc />
    public override Identification Identification => SystemIDs.FinalizationGroup;
}