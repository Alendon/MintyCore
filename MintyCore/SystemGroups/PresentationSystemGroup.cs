using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.SystemGroups;

/// <summary>
///     Root system group for presentation, only executed client side
/// </summary>
[RegisterSystem("presentation_group")]
[ExecutionSide(GameType.Client)]
[RootSystemGroup]
[ExecuteAfter<FinalizationSystemGroup>]
public class PresentationSystemGroup : ARenderSystemGroup
{
    /// <inheritdoc />
    public override Identification Identification => SystemIDs.PresentationGroup;
}