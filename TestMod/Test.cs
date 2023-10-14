using JetBrains.Annotations;
using MintyCore;
using MintyCore.ECS;
using MintyCore.Modding;
using MintyCore.Network;
using MintyCore.Render;
using MintyCore.Render.Managers;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using Silk.NET.Vulkan;
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

    public void Dispose()
    {
        //Nothing to do here
    }

    public ushort ModId { get; set; }

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

    private void GameLoop()
    {
        //If this is a client game (client or local) wait until the player is connected
        while (MathHelper.IsBitSet((int) Engine.GameType, (int) GameType.Client) &&
               PlayerHandler.LocalPlayerGameId == Constants.InvalidId)
            NetworkHandler.Update();

        Engine.DeltaTime = 0;
        Engine.Timer.Reset();
        while (!Engine.Stop)
        {
            Engine.Timer.Tick();

            var simulationEnable = Engine.Timer.GameUpdate(out var deltaTime);
            Engine.Window?.DoEvents(deltaTime);
            
            Engine.DeltaTime = deltaTime;

            var drawingEnable = false;
            if (MathHelper.IsBitSet((int) Engine.GameType, (int) GameType.Client))
            {
                drawingEnable = Engine.Timer.RenderUpdate(out var renderDeltaTime) && VulkanEngine.PrepareDraw();
                Engine.RenderDeltaTime = renderDeltaTime;
            }

            WorldHandler.UpdateWorlds(GameType.Local, simulationEnable, drawingEnable);


            if (drawingEnable)
            {
                var cb = VulkanEngine.GetSecondaryCommandBuffer();

                var swapchainExtent = VulkanEngine.SwapchainExtent;
                var viewport = new Viewport()
                {
                    Height = swapchainExtent.Height,
                    Width = swapchainExtent.Width,
                    MaxDepth = 1
                };
                var scissor = new Rect2D(default, swapchainExtent);

                VulkanEngine.Vk.CmdSetViewport(cb, 0, 1, viewport);
                VulkanEngine.Vk.CmdSetScissor(cb, 0, 1, scissor);

                var pipeline = PipelineManager.GetPipeline(PipelineIDs.Triangle);
                VulkanEngine.Vk.CmdBindPipeline(cb, PipelineBindPoint.Graphics, pipeline);
                VulkanEngine.Vk.CmdDraw(cb, 3, 1, 0, 0);

                VulkanEngine.ExecuteSecondary(cb);
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
    }

    public void PostLoad()
    {
        //Nothing to do here
    }

    public void Unload()
    {
    }
}