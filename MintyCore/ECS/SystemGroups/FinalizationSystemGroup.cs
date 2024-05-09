using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.ECS.SystemGroups;

/// <summary>
///     Root system group for finalization
/// </summary>
[RegisterSystem("finalization_group")]
[RootSystemGroup]
[ExecuteAfter<SimulationSystemGroup>]
public class FinalizationSystemGroup : ASystemGroup
{
    /// <inheritdoc />
    public override Identification Identification => SystemIDs.FinalizationGroup;
}