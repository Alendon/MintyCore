using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using MintyCore.Registries;

namespace MintyCore.SystemGroups;

/// <summary>
///     Root system group for all simulation systems. Default if no system groups is set
/// </summary>
[RegisterSystem("simulation_group")]
[RootSystemGroup]
[ExecuteAfter(typeof(InitializationSystemGroup))]
public class SimulationSystemGroup : ASystemGroup
{
    /// <inheritdoc />
    public override Identification Identification => SystemIDs.SimulationGroup;
}