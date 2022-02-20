using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.SystemGroups;

/// <summary>
///     Root system group for presentation, only executed client side
/// </summary>
[ExecutionSide(GameType.CLIENT)]
[RootSystemGroup]
[ExecuteAfter(typeof(FinalizationSystemGroup))]
public class PresentationSystemGroup : ASystemGroup
{
    /// <inheritdoc />
    public override Identification Identification => SystemGroupIDs.Presentation;
}