using System.Numerics;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Registries;
using MintyCore.Utils;
using TestMod.Identifications;

namespace TestMod;

[RegisterSystem("test")]
public partial class TestSystem(IGameTimer gameTimer) : ASystem
{
    public override Identification Identification => SystemIDs.Test;
    [ComponentQuery] private readonly ComponentQuery<Position> _positionQuery = new();
    
    public override void Setup(SystemManager systemManager)
    {
        _positionQuery.Setup(this);
    }

    protected override void Execute()
    {
        foreach (var entity in _positionQuery)
        {
            entity.GetPosition().Value += new Vector3(1, 0.5f, 0) * gameTimer.DeltaTime;
        }
    }
}