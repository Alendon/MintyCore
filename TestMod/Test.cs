using System.Diagnostics;
using System.Numerics;
using Avalonia.Threading;
using JetBrains.Annotations;
using MintyCore;
using MintyCore.AvaloniaIntegration;
using MintyCore.ECS;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Network;
using MintyCore.Registries;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using TestMod.Identifications;
using TestMod.Render;
using RenderInputDataIDs = TestMod.Identifications.RenderInputDataIDs;

namespace TestMod;

[UsedImplicitly]
public sealed class Test : IMod
{
    public required IModManager ModManager { [UsedImplicitly] init; private get; }
    public required IWorldHandler WorldHandler { [UsedImplicitly] init; private get; }
    public required IPlayerHandler PlayerHandler { [UsedImplicitly] init; private get; }
    public required ITextureManager TextureManager { [UsedImplicitly] init; private get; }
    public required IVulkanEngine VulkanEngine { [UsedImplicitly] init; private get; }
    public required INetworkHandler NetworkHandler { private get; init; }
    public required IRenderManager RenderManager { private get; init; }

    public required IInputDataManager InputDataManager { [UsedImplicitly] init; private get; }
    public required ITestDependency TestDependency { [UsedImplicitly] init; private get; }
    public required IAvaloniaController AvaloniaController { [UsedImplicitly] init; private get; }
    public required IInputHandler InputHandler { [UsedImplicitly] init; private get; }

    public void Dispose()
    {
        //Nothing to do here
    }

    public void PreLoad()
    {
        TestDependency.DoSomething();

        Engine.RunMainMenu = RunMainMenu;
        Engine.RunHeadless = RunHeadless;

        VulkanEngine.AddDeviceFeatureExension(new PhysicalDeviceShaderDrawParametersFeatures
        {
            SType = StructureType.PhysicalDeviceShaderDrawParametersFeatures,
            ShaderDrawParameters = Vk.True
        });
    }

    private void RunHeadless()
    {
        Log.Information("Welcome to the TestMod Headless!");
        Engine.SetGameType(GameType.Server);
        Engine.LoadMods(ModManager.GetAvailableMods(true));
        WorldHandler.CreateWorlds(GameType.Server);
        Engine.CreateServer(Engine.HeadlessPort);

        GameLoop();
    }

    private void RunMainMenu()
    {
        Log.Information("Welcome to the TestMod MainMenu!");
        //TODO add a way to connect to a server
        Log.Information("Currently it is only possible to create a local game");
        var texture = TextureManager.GetTexture(TextureIDs.Dirt);
        Log.Information("Test Texture is of size {TextureWidth} x {TextureHeight}",
            texture.Width, texture.Height);
        Engine.SetGameType(GameType.Local);
        PlayerHandler.LocalPlayerId = 1;
        PlayerHandler.LocalPlayerName = "Local";

        Engine.LoadMods(ModManager.GetAvailableMods(true));

        WorldHandler.CreateWorlds(GameType.Server);

        Engine.CreateServer(Constants.DefaultPort);
        Engine.ConnectToServer("localhost", Constants.DefaultPort);

        GameLoop();
    }

    [RegisterArchetype("test")]
    public static ArchetypeInfo TestArchetype() => new()
    {
        Ids = new[]
        {
            ComponentIDs.Position
        }
    };

    private int _currentTriangle;

    private void GameLoop()
    {
        //If this is a client game (client or local) wait until the player is connected
        while (MathHelper.IsBitSet((int)Engine.GameType, (int)GameType.Client) &&
               PlayerHandler.LocalPlayerGameId == Constants.InvalidId)
        {
            NetworkHandler.Update();
            Thread.Sleep(10);
        }

        if (!WorldHandler.TryGetWorld(GameType.Server, WorldIDs.Test, out _))
            throw new Exception("Failed to get world");

        Engine.DeltaTime = 0;
        Engine.Timer.TargetTicksPerSecond = 60;

        Engine.Timer.Reset();

        RenderManager.StartRendering();
        RenderManager.MaxFrameRate = 100;

        Dispatcher.UIThread.Invoke(() =>
            AvaloniaController.TopLevel.Content = new TestControl());

        var sw = Stopwatch.StartNew();

        InputDataManager.SetKeyIndexedInputData(RenderInputDataIDs.TriangleInputData, _currentTriangle++, new Triangle
        {
            Color = Vector3.UnitX,
            Point1 = new Vector3(0, 0, 0),
            Point2 = new Vector3(1, 0, 0),
            Point3 = new Vector3(0, 1, 0)
        });

        while (!Engine.Stop)
        {
            Engine.Timer.Tick();

            var simulationEnable = Engine.Timer.GameUpdate(out var deltaTime);
            Engine.Window?.DoEvents(deltaTime);

            Engine.DeltaTime = deltaTime;

            WorldHandler.UpdateWorlds(GameType.Local, simulationEnable);

            WorldHandler.SendEntityUpdates();

            NetworkHandler.Update();

            var scroll = InputHandler.ScrollWheelDelta;
            if (scroll.Length() > 0)
            {
                Log.Debug("Scroll: {Scroll}", scroll);
            }

            if (sw.Elapsed.TotalSeconds > 1)
            {
                Log.Debug("Current FPS: {Fps}", RenderManager.FrameRate);
                sw.Restart();
                Dispatcher.UIThread.Invoke(() =>
                {
                    if (AvaloniaController.TopLevel.Content is TestControl c)
                        c.LoremIpsum.IsVisible = !c.LoremIpsum.IsVisible;
                });
            }

            if (_createTriangle)
            {
                //create a triangle with random color and position

                var rnd = Random.Shared;

                var triangle = new Triangle
                {
                    Color = new Vector3((float)rnd.NextDouble() + 0.25f, (float)rnd.NextDouble() + 0.25f,
                        (float)rnd.NextDouble() + 0.25f),
                    Point1 = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1, 0),
                    Point2 = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1, 0),
                    Point3 = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1, 0)
                };

                InputDataManager.SetKeyIndexedInputData(RenderInputDataIDs.TriangleInputData, _currentTriangle++,
                    triangle);

                _createTriangle = false;
            }

            if (simulationEnable)
                Engine.Tick++;
        }

        RenderManager.StopRendering();
        Engine.CleanupGame();
    }

    public void Load()
    {
    }

    public void PostLoad()
    {
        //Nothing to do here
    }

    public void Unload()
    {
        Engine.RunHeadless = null!;
        Engine.RunMainMenu = null!;
    }

    private static bool _createTriangle;

    [RegisterKeyAction("create_triangle")]
    public static KeyActionInfo CreateTriangleAction() => new()
    {
        Key = Key.T,
        Action = (state, _) =>
        {
            if (state == KeyStatus.KeyDown) _createTriangle = true;
        }
    };
}