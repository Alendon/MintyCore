using MintyCore.GameStates;
using Serilog;

namespace TestMod;

public class TestGameState(IGameStateMachine gameStateMachine) : GameState<TestGameState.InitializationData>
{
    public override void Initialize(InitializationData data)
    {
        Log.Information("Initializing TestGameState with data: {Data}", data.TestData);
    }
    
    public override void Update()
    {
        gameStateMachine.PopGameState();
    }

    public override void Cleanup()
    {
        
    }
    
    public struct InitializationData
    {
        public string TestData { get; set; }
    }
    
}