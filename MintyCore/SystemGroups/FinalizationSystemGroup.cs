using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.SystemGroups;

/// <summary>
///     Root system group for finalization
/// </summary>
[RootSystemGroup]
[ExecuteAfter(typeof(SimulationSystemGroup))]
public class FinalizationSystemGroup : ASystemGroup
{
    /// <inheritdoc />
    public override Identification Identification => SystemGroupIDs.Finalization;
}