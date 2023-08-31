using JetBrains.Annotations;
using MintyCore;
using MintyCore.ECS;
using MintyCore.Modding;
using MintyCore.Network;
using MintyCore.Render;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using TestMod.Identifications;

namespace TestMod;

[UsedImplicitly]
public sealed partial class Test : IMod
{
    public static Test? Instance { get; private set; }

    public void Dispose()
    {
        //Nothing to do here
    }

    public ushort ModId { get; set; }

    public void PreLoad()
    {
        Instance = this;

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

        var texture = TextureHandler.GetTexture(TextureIDs.Dirt);
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

    private static void GameLoop()
    {
        //If this is a client game (client or local) wait until the player is connected
        while (MathHelper.IsBitSet((int)Engine.GameType, (int)GameType.Client) &&
               PlayerHandler.LocalPlayerGameId == Constants.InvalidId)
            NetworkHandler.Update();

        Engine.DeltaTime = 0;
        Engine.Timer.Reset();
        while (!Engine.Stop)
        {
            Engine.Timer.Tick();
            Engine.Window?.DoEvents();

            var simulationEnable = Engine.Timer.GameUpdate(out var deltaTime);
            Engine.DeltaTime = deltaTime;
            
            var drawingEnable = false;
            if(MathHelper.IsBitSet((int)Engine.GameType, (int)GameType.Client))
            {
                drawingEnable = Engine.Timer.RenderUpdate(out var renderDeltaTime) && VulkanEngine.PrepareDraw();
                Engine.RenderDeltaTime = renderDeltaTime;
            }

            WorldHandler.UpdateWorlds(GameType.Local, simulationEnable, drawingEnable);


            if (drawingEnable)
            {
                VulkanEngine.EndDraw();
            }

            WorldHandler.SendEntityUpdates();

            NetworkHandler.Update();


            Logger.AppendLogToFile();
            if (simulationEnable)
                Engine.Tick++;
        }
        
        Engine.CleanupGame();
    }

    public void Load()
    {
        InternalRegister();
    }

    public void PostLoad()
    {
        //Nothing to do here
    }

    public void Unload()
    {
        InternalUnregister();
    }
}