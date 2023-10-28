using System.Diagnostics;
using JetBrains.Annotations;
using MintyCore;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Network;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using Serilog;
using TestMod.Identifications;

namespace TestMod;

[UsedImplicitly]
public sealed class Test : IMod
{
    public required IModManager ModManager { [UsedImplicitly] init; private get; }
    public required IWorldHandler WorldHandler { [UsedImplicitly] init; private get; }
    public required IPlayerHandler PlayerHandler { [UsedImplicitly] init; private get; }
    public required ITextureManager TextureManager { [UsedImplicitly] init; private get; }
    public required IVulkanEngine VulkanEngine { [UsedImplicitly] init; private get; }
    public required IPipelineManager PipelineManager { [UsedImplicitly] init; private get; }
    public required INetworkHandler NetworkHandler { private get; init; }
    
    public required IRenderManager RenderManager { private get; init; }

    public void Dispose()
    {
        //Nothing to do here
    }
    
    public void PreLoad()
    {
        Engine.RunMainMenu = RunMainMenu;

        Engine.RunHeadless = RunHeadless;
    }

    private void RunHeadless()
    {
        Logger.WriteLog("Welcome to the TestMod Headless!", LogImportance.Info, "TestMod");

        Engine.SetGameType(GameType.Server);
        Engine.LoadMods(ModManager.GetAvailableMods(true));
        WorldHandler.CreateWorlds(GameType.Server);
        Engine.CreateServer(Engine.HeadlessPort);

        GameLoop();
    }

    private void RunMainMenu()
    {
        Logger.WriteLog("Welcome to the TestMod MainMenu!", LogImportance.Info, "TestMod");
        //TODO add a way to connect to a server
        Logger.WriteLog("Currently it is only possible to create a local game", LogImportance.Info, "TestMod");

        var texture = TextureManager.GetTexture(TextureIDs.Dirt);
        Logger.WriteLog($"Test Texture is of size {texture.Width} x {texture.Height}", LogImportance.Info, "TestMod");

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
        Ids = new []
        {
            ComponentIDs.Position
        }
    };

    private void GameLoop()
    {
        //If this is a client game (client or local) wait until the player is connected
        while (MathHelper.IsBitSet((int) Engine.GameType, (int) GameType.Client) &&
               PlayerHandler.LocalPlayerGameId == Constants.InvalidId)
            NetworkHandler.Update();

        Logger.AssertAndThrow(WorldHandler.TryGetWorld(GameType.Server, WorldIDs.Test, out var world), "Failed to get world", "TestMod");
        
        Engine.DeltaTime = 0;
        Engine.Timer.TargetTicksPerSecond = 60;
        
        Engine.Timer.Reset();
        
        RenderManager.SetRenderModuleActive(RenderModuleIDs.FillColor, true);
        RenderManager.StartRendering();

        var sw = Stopwatch.StartNew();
        
        while (!Engine.Stop)
        {
            Engine.Timer.Tick();

            var simulationEnable = Engine.Timer.GameUpdate(out var deltaTime);
            Engine.Window?.DoEvents(deltaTime);
            
            Engine.DeltaTime = deltaTime;

            WorldHandler.UpdateWorlds(GameType.Local, simulationEnable, true);

            WorldHandler.SendEntityUpdates();

            NetworkHandler.Update();
            
            if (sw.Elapsed.TotalSeconds > 1)
            {
                Log.Debug("Current FPS: {Fps}", RenderManager.FrameRate);
                sw.Restart();
            }

            Logger.AppendLogToFile();
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
}