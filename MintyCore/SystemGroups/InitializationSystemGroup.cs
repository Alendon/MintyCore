using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.SystemGroups;

/// <summary>
///     Root system group for initialization
/// </summary>
[RootSystemGroup]
public class InitializationSystemGroup : ASystemGroup
{
    /// <inheritdoc />
    public override Identification Identification => SystemGroupIDs.Initialization;
}