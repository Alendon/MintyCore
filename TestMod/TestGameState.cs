using MintyCore.GameStates;
using MintyCore.Registries;
using Serilog;

namespace TestMod;

public class TestGameState(IGameStateMachine gameStateMachine) : GameState<TestGameState.InitializationData>
{
    private InitializationData? _data;
    
    public override void Initialize(InitializationData data)
    {
        _data = data;
        Log.Information("Initializing TestGameState with data: {Data}", data.TestData);
    }
    
    public override void Update()
    {
        gameStateMachine.PopGameState();
    }

    public override void Cleanup(bool restorable)
    {
        if(!restorable)
            _data = null;
    }

    public override void Restore()
    {
        Initialize(_data!.Value);
    }

    public struct InitializationData
    {
        public string TestData { get; set; }
    }
    
    
    [RegisterGameState("test")]
    public static GameStateDescription<TestGameState> Description => new();
}