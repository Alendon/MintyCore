using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.ECS.SystemGroups;

/// <summary>
///     Root system group for all simulation systems. Default if no system groups is set
/// </summary>
[RegisterSystem("simulation_group")]
[RootSystemGroup]
[ExecuteAfter<InitializationSystemGroup>]
public class SimulationSystemGroup : ASystemGroup
{
    /// <inheritdoc />
    public override Identification Identification => SystemIDs.SimulationGroup;
}