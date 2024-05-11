using MintyCore.ECS;
using MintyCore.Registries;
using MintyCore.Utils;
using Serilog;

namespace TestMod.Components;

[RegisterComponentSerializer("test")]
public class TestComponentSerializer(IGameTimer timer) : ComponentSerializer<TestComponent>
{
    public override void Serialize(ref TestComponent component, DataWriter writer, IWorld world, Entity entity)
    {
        //Log.Information("Serializing TestComponent {Seconds} seconds after game start", timer.ElapsedTimeSinceStart.TotalSeconds);
    }

    public override bool Deserialize(ref TestComponent component, DataReader reader, IWorld world, Entity entity)
    {
        return true;
    }
}