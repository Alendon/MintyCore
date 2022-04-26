using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using MintyCore.Registries;

namespace MintyCore.SystemGroups;

/// <summary>
///     Root system group for presentation, only executed client side
/// </summary>
[RegisterSystem("presentation_group")]
[ExecutionSide(GameType.Client)]
[RootSystemGroup]
[ExecuteAfter(typeof(FinalizationSystemGroup))]
public class PresentationSystemGroup : ASystemGroup
{
    /// <inheritdoc />
    public override Identification Identification => SystemIDs.PresentationGroup;
}