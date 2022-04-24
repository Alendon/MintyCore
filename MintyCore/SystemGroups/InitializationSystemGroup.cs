using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using MintyCore.Registries;

namespace MintyCore.SystemGroups;

/// <summary>
///     Root system group for initialization
/// </summary>
[RegisterSystem("initialization_group")]
[RootSystemGroup]
public class InitializationSystemGroup : ASystemGroup
{
    /// <inheritdoc />
    public override Identification Identification => SystemIDs.InitializationGroup;
}