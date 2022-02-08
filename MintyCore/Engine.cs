using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ENet;
using MintyCore.ECS;
using MintyCore.Modding;
using MintyCore.Network;
using MintyCore.Network.Messages;
using MintyCore.Render;
using MintyCore.UI;
using MintyCore.Utils;

namespace MintyCore;

/// <summary>
///     Engine/CoreGame main class
/// </summary>
public static class Engine
{
    /// <summary>
    /// Generic delegate for all player events with the player id and whether or not the event was fired server side
    /// </summary>
    public delegate void PlayerEvent(ushort playerGameId, bool serverSide);

    /// <summary>
    /// The maximum tick count before the tick counter will be set to 0, range 0 - (<see cref="MaxTickCount"/> - 1)
    /// </summary>
    public const int MaxTickCount = 1_000_000_000;

    private static readonly Stopwatch _tickTimeWatch = new();
    
    private static readonly List<DirectoryInfo> _additionalModDirectories = new();

    internal static bool ShouldStop;

    private static readonly object _playersLock = new();
    private static readonly Dictionary<ushort, ulong> _playerIDs = new();
    private static readonly Dictionary<ushort, string> _playerNames = new();

    /// <summary>
    /// The server world
    /// </summary>
    public static World? ServerWorld { get; private set; }

    /// <summary>
    /// The client world
    /// </summary>
    public static World? ClientWorld { get; private set; }

    /// <summary>
    ///     The <see cref="GameType" /> of the running instance
    /// </summary>
    public static GameType GameType { get; private set; } = GameType.INVALID;

    /// <summary>
    ///     The reference to the main <see cref="Window" />
    /// </summary>
    public static Window? Window { get; private set; }

    /// <summary>
    ///     The delta time of the current tick as double in Seconds
    /// </summary>
    public static double DDeltaTime { get; private set; }

    /// <summary>
    ///     The delta time of the current tick in Seconds
    /// </summary>
    public static float DeltaTime { get; private set; }

    /// <summary>
    ///     Fixed delta time for physics simulation in Seconds
    /// </summary>
    public static float FixedDeltaTime => 0.02f;

    /// <summary>
    ///     The current Tick number. Capped between 0 and <see cref="MaxTickCount"/> (exclusive)
    /// </summary>
    public static int Tick { get; private set; }

    internal static bool Stop => ShouldStop || Window is not null && !Window.Exists;

    /// <summary>
    /// The game id of the local player
    /// </summary>
    public static ushort LocalPlayerGameId { get; internal set; } = Constants.InvalidId;

    /// <summary>
    /// The global id of the local player
    /// </summary>
    public static ulong LocalPlayerId { get; internal set; } = Constants.InvalidId;

    /// <summary>
    /// The name of the local player
    /// </summary>
    public static string LocalPlayerName { get; internal set; } = "Player";

    /// <summary>
    /// The entry/main method of the engine
    /// </summary>
    /// <remarks>Is public to allow easier mod development</remarks>
    public static void Main(string[] args)
    {
        CheckProgramArguments(args);

        Init();

        RunMainMenu();
        //DirectLocalGame();
        //MainMenu();
        CleanUp();
    }

    private static void RunMainMenu()
    {
        MainMenu mainMenu = new(DirectLocalGame);
        mainMenu.Initialize();
        mainMenu.IsActive = true;
        MainUiRenderer.SetMainUiContext(mainMenu);
        
        while (Window is not null && Window.Exists)
        {
            SetDeltaTime();

            Window!.DoEvents();
            
            UiHandler.Update();
            UiHandler.UpdateElement(mainMenu);

            VulkanEngine.PrepareDraw();
            
            MainUiRenderer.DrawMainUi();
            
            VulkanEngine.EndDraw();
        }
        mainMenu.Dispose();
    }

    private static void DirectLocalGame()
    {
        LocalPlayerId = 1;
        LocalPlayerName = "Local";

        GameType = GameType.LOCAL;
        ShouldStop = false;

        ModManager.LoadGameMods(ModManager.GetAvailableMods());

        LoadServerWorld();

        NetworkHandler.StartServer(5665, 16);

        Address address = new() { Port = 5665 };
        address.SetHost("localhost");
        NetworkHandler.ConnectToServer(address);

        while (LocalPlayerGameId == Constants.InvalidId) NetworkHandler.Update();

        GameLoop();

        VulkanEngine.WaitForAll();

        NetworkHandler.StopClient();
        NetworkHandler.StopServer();

        ServerWorld?.Dispose();
        ClientWorld?.Dispose();

        ServerWorld = null;
        ClientWorld = null;

        GameType = GameType.INVALID;

        OnServerWorldCreate = delegate { };
        OnClientWorldCreate = delegate { };
        BeforeWorldTicking = delegate { };
        AfterWorldTicking = delegate { };
        OnPlayerConnected = delegate { };
        OnPlayerDisconnected = delegate { };

        ModManager.UnloadMods(false);
    }


    private static void Init()
    {
        Thread.CurrentThread.Name = "MintyCoreMain";

        Logger.InitializeLog();

        Library.Initialize();
        Window = new Window();

        VulkanEngine.Setup();

        ModManager.SearchMods(_additionalModDirectories);
        ModManager.LoadRootMods();
        MainUiRenderer.SetupMainUiRendering();
    }

    private static void CheckProgramArguments(string[] args)
    {
        foreach (var argument in args)
        {
            if (argument.Length == 0 || argument[0] != '-') continue;

            if (!argument.StartsWith("-addModDir=")) continue;

            var modDir = argument["-addModDir=".Length..];
            DirectoryInfo dir = new(modDir);
            if (dir.Exists) _additionalModDirectories.Add(dir);
        }
    }

    internal static void LoadServerWorld()
    {
        ServerWorld = new World(true);
        ServerWorld.SetupTick();

        OnServerWorldCreate();
    }

    /// <summary>
    /// Event which gets fired before the worlds ticks
    /// </summary>
    public static event Action BeforeWorldTicking = delegate { };

    /// <summary>
    /// Event which gets fired after the worlds ticks
    /// </summary>
    public static event Action AfterWorldTicking = delegate { };

    /// <summary>
    /// Event which gets fired when the server world gets created
    /// </summary>
    public static event Action OnServerWorldCreate = delegate { };

    /// <summary>
    /// Event which gets fired when the client world gets created
    /// </summary>
    public static event Action OnClientWorldCreate = delegate { };

    /// <summary>
    /// Event which gets fired when a player connects. May not be fired from the main thread!
    /// </summary>
    public static event PlayerEvent OnPlayerConnected = delegate { };

    /// <summary>
    /// Event which gets fired when a player disconnects. May not be fired from the main thread!
    /// </summary>
    public static event PlayerEvent OnPlayerDisconnected = delegate { };
    


    private static void GameLoop()
    {
        var serverUpdateDic = new Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>>();
        var clientUpdateDic = new Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>>();

        while (Stop == false)
        {
            SetDeltaTime();

            Window!.DoEvents();

            VulkanEngine.PrepareDraw();

            BeforeWorldTicking();

            ServerWorld?.Tick();
            ClientWorld?.Tick();

            AfterWorldTicking();
            
            VulkanEngine.EndDraw();

            foreach (var archetypeId in ArchetypeManager.GetArchetypes().Keys)
            {
                var serverStorage = ServerWorld?.EntityManager.GetArchetypeStorage(archetypeId);
                var clientStorage = ClientWorld?.EntityManager.GetArchetypeStorage(archetypeId);

                if (serverStorage is not null)
                {
                    ArchetypeStorage.DirtyComponentQuery dirtyComponentQuery = new(serverStorage);
                    while (dirtyComponentQuery.MoveNext())
                    {
                        var component = dirtyComponentQuery.Current;
                        if (ComponentManager.IsPlayerControlled(component.ComponentId)) continue;

                        if (!serverUpdateDic.ContainsKey(component.Entity))
                            serverUpdateDic.Add(component.Entity,
                                new List<(Identification componentId, IntPtr componentData)>());
                        serverUpdateDic[component.Entity].Add((component.ComponentId, component.ComponentPtr));
                    }
                }

                if (clientStorage is not null)
                {
                    ArchetypeStorage.DirtyComponentQuery dirtyComponentQuery = new(clientStorage);
                    while (dirtyComponentQuery.MoveNext())
                    {
                        var component = dirtyComponentQuery.Current;
                        if (!ComponentManager.IsPlayerControlled(component.ComponentId)) continue;

                        if (!clientUpdateDic.ContainsKey(component.Entity))
                            clientUpdateDic.Add(component.Entity,
                                new List<(Identification componentId, IntPtr componentData)>());
                        clientUpdateDic[component.Entity].Add((component.ComponentId, component.ComponentPtr));
                    }
                }
            }

            if (GameType.HasFlag(GameType.SERVER))
            {
                ComponentUpdate message = new()
                {
                    Components = serverUpdateDic,
                    World = ServerWorld
                };
                message.Send(_playerIDs.Keys);
            }

            if (GameType.HasFlag(GameType.CLIENT))
            {
                ComponentUpdate message = new()
                {
                    Components = clientUpdateDic,
                    World = ClientWorld
                };
                message.SendToServer();
            }

            foreach (var updateValues in clientUpdateDic.Values) updateValues.Clear();

            foreach (var updateValues in serverUpdateDic.Values) updateValues.Clear();

            NetworkHandler.Update();


            Logger.AppendLogToFile();
            Tick = (Tick + 1) % MaxTickCount;
        }
    }

    private static void SetDeltaTime()
    {
        _tickTimeWatch.Stop();
        DDeltaTime = _tickTimeWatch.Elapsed.TotalSeconds;
        DeltaTime = (float)_tickTimeWatch.Elapsed.TotalSeconds;
        _tickTimeWatch.Restart();
    }

    private static void CleanUp()
    {
        VulkanEngine.WaitForAll();
        MainUiRenderer.DestroyMainUiRendering();
        ModManager.UnloadMods(true);

        Library.Deinitialize();
        MemoryManager.Clear();
        VulkanEngine.Shutdown();
        AllocationHandler.CheckUnFreed();
    }

    /// <summary>
    /// Get all connected players
    /// </summary>
    /// <returns>IEnumerable containing the player game ids</returns>
    public static IEnumerable<ushort> GetConnectedPlayers()
    {
        Dictionary<ushort, ulong>.KeyCollection players;
        lock (_playersLock)
        {
            players = _playerIDs.Keys;
        }

        return players;
    }

    /// <summary>
    /// Get the name of a player
    /// </summary>
    /// <param name="gameId">The player game id</param>
    /// <returns>Player name</returns>
    public static string GetPlayerName(ushort gameId)
    {
        string name;
        lock (_playersLock)
        {
            name = _playerNames[gameId];
        }

        return name;
    }

    /// <summary>
    /// Get the player global id
    /// </summary>
    /// <param name="gameId">The player game id</param>
    /// <returns></returns>
    public static ulong GetPlayerId(ushort gameId)
    {
        ulong id;
        lock (_playersLock)
        {
            id = _playerIDs[gameId];
        }

        return id;
    }

    internal static void DisconnectPlayer(ushort player, bool serverSide)
    {
        OnPlayerDisconnected(player, serverSide);
        RemovePlayer(player);
        if (serverSide && GameType != GameType.LOCAL)
        {
            RemovePlayerEntities(player);
            PlayerLeft message = new();
            message.PlayerGameId = player;
            message.Send(_playerIDs.Keys);
        }
    }

    internal static void RemovePlayer(ushort playerId)
    {
        lock (_playersLock)
        {
            _playerIDs.Remove(playerId);
            _playerNames.Remove(playerId);
        }
    }

    private static void RemovePlayerEntities(ushort playerId)
    {
        if (ServerWorld is null) return;
        foreach (var entity in ServerWorld.EntityManager.GetEntitiesByOwner(playerId))
            ServerWorld.EntityManager.DestroyEntity(entity);
    }

    internal static void AddPlayer(ushort gameId, string playerName, ulong playerId, bool serverSide)
    {
        lock (_playersLock)
        {
            if (_playerIDs.ContainsKey(gameId)) return;

            _playerIDs.Add(gameId, playerId);
            _playerNames.Add(gameId, playerName);
        }

        OnPlayerConnected(gameId, serverSide);
    }


    internal static bool AddPlayer(string playerName, ulong playerId, out ushort id, bool serverSide)
    {
        lock (_playersLock)
        {
            id = Constants.ServerId + 1;
            while (_playerIDs.ContainsKey(id)) id++;

            _playerIDs.Add(id, playerId);
            _playerNames.Add(id, playerName);
        }

        OnPlayerConnected(id, serverSide);
        return true;
    }

    internal static void CreateClientWorld()
    {
        ClientWorld = new World(false);
        ClientWorld.SetupTick();

        OnClientWorldCreate();
    }
}