using System.Diagnostics;
using System.Numerics;
using ENet;
using JetBrains.Annotations;
using MintyCore;
using MintyCore.ECS;
using MintyCore.GameStates;
using MintyCore.Graphics;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Input;
using MintyCore.Modding;
using MintyCore.Network;
using MintyCore.Registries;
using MintyCore.UI;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using Serilog;
using Silk.NET.GLFW;
using TestMod.Identifications;
using TestMod.Render;

namespace TestMod;

[UsedImplicitly]
public class MainMenuGameState(
    IEngineConfiguration engine,
    IPlayerHandler playerHandler,
    IWorldHandler worldHandler,
    INetworkHandler networkHandler,
    IRenderManager renderManager,
    IInputDataManager inputDataManager,
    IViewLocator viewLocator,
    IGameTimer timer,
    IModManager modManager,
    IWindowHandler windowHandler,
    IInputHandler inputHandler,
    IVulkanEngine vulkanEngine) : GameState
{
    private int _currentTriangle;
    private Stopwatch _stopwatch = Stopwatch.StartNew();

    public override void Initialize()
    {
        Log.Information("Welcome to the TestMod MainMenu!");
        engine.SetGameType(GameType.Local);
        playerHandler.LocalPlayerId = 1;
        playerHandler.LocalPlayerName = "Local";

        modManager.LoadGameMods(modManager.GetAvailableMods(true).Where(x => !x.IsRootMod));

        worldHandler.CreateWorlds(GameType.Server);

        networkHandler.StartServer(Constants.DefaultPort, 16);

        var address = new Address() { Port = Constants.DefaultPort };
        address.SetHost("localhost");
        networkHandler.ConnectToServer(address);

        //If this is a client game (client or local) wait until the player is connected
        while (MathHelper.IsBitSet((int)engine.GameType, (int)GameType.Client) &&
               playerHandler.LocalPlayerGameId == Constants.InvalidId)
        {
            networkHandler.Update();
            Thread.Sleep(10);
        }

        if (!worldHandler.TryGetWorld(GameType.Server, WorldIDs.Test, out _))
            throw new Exception("Failed to get world");

        timer.SetTargetTicksPerSecond(60);
        timer.Reset();

        renderManager.StartRendering();
        renderManager.MaxFrameRate = 100;

        viewLocator.SetRootView(ViewIDs.TestMain);

        _stopwatch = Stopwatch.StartNew();

        _currentTriangle = 0;
        inputDataManager.SetKeyIndexedInputData(RenderInputDataIDs.TriangleInputData, _currentTriangle++, new Triangle
        {
            Color = Vector3.UnitX,
            Point1 = new Vector3(0, 0, 0),
            Point2 = new Vector3(1, 0, 0),
            Point3 = new Vector3(0, 1, 0)
        });
    }

    public override void Update()
    {
        timer.Update();
        windowHandler.GetMainWindow().DoEvents(timer.DeltaTime);

        worldHandler.UpdateWorlds(GameType.Local, timer.IsSimulationTick);
        worldHandler.SendEntityUpdates();
        networkHandler.Update();

        var scroll = inputHandler.ScrollWheelDelta;
        if (scroll.Length() > 0)
        {
            Log.Debug("Scroll: {Scroll}, Tick {Tick}", scroll, timer.Tick);
        }

        if (_stopwatch.Elapsed.TotalSeconds > 1)
        {
            Log.Debug("Current FPS: {Fps}", renderManager.FrameRate);
            _stopwatch.Restart();
        }

        if (_createTriangle)
        {
            var rnd = Random.Shared;

            var triangle = new Triangle
            {
                Color = new Vector3((float)rnd.NextDouble() + 0.25f, (float)rnd.NextDouble() + 0.25f,
                    (float)rnd.NextDouble() + 0.25f),
                Point1 = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1, 0),
                Point2 = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1, 0),
                Point3 = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1, 0)
            };

            inputDataManager.SetKeyIndexedInputData(RenderInputDataIDs.TriangleInputData, _currentTriangle++,
                triangle);

            _createTriangle = false;
        }
    }

    public override void Cleanup(bool restorable)
    {
        renderManager.StopRendering();
        vulkanEngine.WaitForAll();

        networkHandler.StopClient();
        networkHandler.StopServer();

        worldHandler.DestroyWorlds(GameType.Local);

        modManager.UnloadMods(false);
        timer.Reset();
    }

    public override void Restore()
    {
        Initialize();
    }

    [RegisterGameState("main_menu")] public static GameStateDescription<MainMenuGameState> Description => new();

    private static bool _createTriangle;

    [RegisterInputAction("create_triangle")]
    public static InputActionDescription CreateTriangleAction() => new()
    {
        DefaultInput = Keys.T,
        ActionCallback = inputActionParams =>
        {
            if (inputActionParams.InputAction == InputAction.Press) _createTriangle = true;
            return InputActionResult.Stop;
        }
    };
}