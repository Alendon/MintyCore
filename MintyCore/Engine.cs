using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Autofac;
using ENet;
using JetBrains.Annotations;
using MintyCore.ECS;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Utils;
using MintyCore.Modding;
using MintyCore.Modding.Implementations;
using MintyCore.Network;
using MintyCore.UI;
using MintyCore.Utils;
using Myra;
using Myra.Graphics2D.UI;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using EnetLibrary = ENet.Library;
using Timer = MintyCore.Utils.Timer;
using Window = MintyCore.Utils.Window;

namespace MintyCore;

/// <summary>
///     Engine/CoreGame main class
/// </summary>
[PublicAPI]
public static class Engine
{
    /// <summary>
    /// If true the engine will start without all graphics features. (Console only, no window, no vulkan)
    /// </summary>
    public static bool HeadlessModeActive { get; set; }

    public static ushort HeadlessPort { get; set; } = Constants.DefaultPort;

    private static readonly List<DirectoryInfo> _additionalModDirectories = new();

    /// <summary>
    /// 
    /// </summary>
    public static bool ShouldStop;

    public static ModState ModState;

    /// <summary>
    /// Indicates whether tests should be active. Meant to replace DEBUG compiler flags
    /// </summary>
    public static bool TestingModeActive { get; set; }
#if DEBUG
        = true;
#endif

    /// <summary>
    /// Timer instance used for the main game loop
    /// </summary>
    public static readonly Timer Timer = new();

    /// <summary>
    ///     The <see cref="GameType" /> of the running instance
    /// </summary>
    public static GameType GameType { get; set; } = GameType.None;

    /// <summary>
    ///     The reference to the main <see cref="Window" />
    /// </summary>
    public static Window? Window { get; private set; }

    public static Desktop? Desktop { get; private set; }

    /// <summary>
    ///     The delta time of the current tick in Seconds
    /// </summary>
    public static float DeltaTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public static int TargetFps
    {
        get => Timer.TargetFps;
        set => Timer.TargetFps = value;
    }

    /// <summary>
    /// 
    /// </summary>
    public static int CurrentFps => Timer.RealFps;

    /// <summary>
    ///     Fixed delta time for physics simulation in Seconds
    /// </summary>
    public static float FixedDeltaTime => 0.02f;

    /// <summary>
    ///     The current Tick number
    /// </summary>
    public static ulong Tick { get; set; }

    public static bool Stop => ShouldStop || (Window is not null && !Window.Exists);

    /// <summary>
    /// 
    /// </summary>
    public static string[] CommandLineArguments { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// 
    /// </summary>
    public static Action RunMainMenu = () => { };

    /// <summary>
    /// 
    /// </summary>
    public static Action RunHeadless = () => { };

    private static IContainer _container = null!;

    /// <summary>
    ///     The entry/main method of the engine
    /// </summary>
    /// <remarks>Is public to allow easier mod development</remarks>
    public static void Main(string[] args)
    {
        CommandLineArguments = args;
        CheckProgramArguments();
        CreateLogger();
        Logger.InitializeLog();

        //try
        {
            Init();
            RunGame();
            CleanUp();
        }
        /*catch (Exception e)
        {
            Log.Fatal(e, "Exception occurred while running game");
            Log.CloseAndFlush();
            throw;
        }*/
        Log.CloseAndFlush();
    }

    private static void BuildRootDiContainer()
    {
        var builder = new ContainerBuilder();

        var contextFlags = SingletonContextFlags.None;
        if (!HeadlessModeActive)
            contextFlags |= SingletonContextFlags.NoHeadless;

        builder.RegisterMarkedSingletons(typeof(Engine).Assembly, contextFlags);

        _container = builder.Build();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunGame()
    {
        if (!HeadlessModeActive)
            RunMainMenu();
        else
            RunHeadless();
    }

    private static GameType? overrideGameType;
    internal static GameType RegistryGameType => overrideGameType ?? GameType;

    private static void CreateLogger()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithExceptionDetails()
            .Enrich.FromLogContext()
            .WriteTo.File(new CompactJsonFormatter(), $"log/{timestamp}.log", rollOnFileSizeLimit: true,
                flushToDiskInterval: TimeSpan.FromMinutes(1))
            .WriteTo.Console(outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}][{RootNamespace}/{Class}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static void Init()
    {
        Thread.CurrentThread.Name = "MintyCoreMain";

        Log.Information("Initializing Engine");

        BuildRootDiContainer();

        EnetLibrary.Initialize();

        var modManager = _container.Resolve<IModManager>();

        modManager.SearchMods(_additionalModDirectories);
        modManager.LoadRootMods();

        //As the loading of the root mods is done before the game actually starts, we do not know whether a local game or a client game is started
        //The important thing is to not load objects which needs rendering with the headless mode active
        //As a temporary workaround we just set a override gametype which the registry manager will use
        overrideGameType = HeadlessModeActive ? GameType.Server : GameType.Local;

        modManager.ProcessRegistry(true, LoadPhase.Pre);

        if (!HeadlessModeActive)
        {
            var vulkanEngine = _container.Resolve<IVulkanEngine>();
            var awaiter = _container.Resolve<IAsyncFenceAwaiter>();
            var inputHandler = _container.Resolve<IInputHandler>();

            Window = new Window(inputHandler);
            vulkanEngine.Setup();
            awaiter.Start();
        }

        modManager.ProcessRegistry(true, LoadPhase.Main);

        //Ui Initialization
        //Must happen after the registry is processed
        if (!HeadlessModeActive)
        {
            var platform = _container.Resolve<IUiPlatform>();
            platform.Resize(Window!.FramebufferSize);
            MyraEnvironment.Platform = platform;
            Window!.WindowInstance.Resize += platform.Resize;

            Desktop = new Desktop();
        }

        modManager.ProcessRegistry(true, LoadPhase.Post);

        overrideGameType = null;
    }

    private static void CheckProgramArguments()
    {
        TestingModeActive |= HasCommandLineValue("testingModeActive");

        HeadlessModeActive = HasCommandLineValue("headless");

        var portResult = GetCommandLineValues("port");
        if (ushort.TryParse(portResult.FirstOrDefault(), out var port))
        {
            HeadlessPort = port;
        }

        var modDirResult = GetCommandLineValues("addModDir");
        foreach (var modDir in modDirResult)
        {
            DirectoryInfo dir = new(modDir);
            if (dir.Exists) _additionalModDirectories.Add(dir);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static IEnumerable<string> GetCommandLineValues(string key, char? separator = null)
    {
        if (!key.StartsWith('-')) key = $"-{key}";
        if (!key.EndsWith('=')) key = $"{key}=";

        var values = CommandLineArguments.Where(arg => arg.StartsWith(key))
            .Select(arg => arg.Replace(key, string.Empty));

        if (separator is not null)
            values = values.SelectMany(arg => arg.Split(separator.Value));

        return values;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool HasCommandLineValue(string key)
    {
        if (!key.StartsWith('-')) key = $"-{key}";
        //check if the key is in the arguments

        return Array.Exists(CommandLineArguments, arg => arg == key);
    }

    /// <summary>
    ///     Load the given mods
    /// </summary>
    /// <param name="modsToLoad">The mods to load</param>
    public static void LoadMods(IEnumerable<ModManifest> modsToLoad)
    {
        var modManager = _container.Resolve<IModManager>();
        modManager.LoadGameMods(modsToLoad);
    }

    /// <summary>
    ///     Create a server with the given parameters
    /// </summary>
    /// <param name="port">The port the server should run on</param>
    /// <param name="playerCount">The maximum player count</param>
    public static void CreateServer(ushort port, int playerCount = 16)
    {
        var networkHandler = _container.Resolve<INetworkHandler>();
        networkHandler.StartServer(port, playerCount);
    }

    /// <summary>
    ///     Connect to a server with the given address
    /// </summary>
    /// <param name="address">The address of the server</param>
    /// <param name="port">The port the server listens to</param>
    public static void ConnectToServer(string address, ushort port)
    {
        var networkHandler = _container.Resolve<INetworkHandler>();

        Address targetAddress = new() {Port = port};

        if (!targetAddress.SetHost(address))
        {
            throw new MintyCoreException($"Failed to bind address {address}");
        }

        networkHandler.ConnectToServer(targetAddress);
    }

    /// <summary>
    ///     Set the type of the current game
    ///     Only usable if the game is not running
    /// </summary>
    /// <param name="gameType">The type to set to</param>
    public static void SetGameType(GameType gameType)
    {
        if (gameType is GameType.None or > GameType.Local)
        {
            Log.Error("Tried to set invalid game type {GameType}", gameType);
            return;
        }

        if (GameType != GameType.None)
        {
            Log.Error("Cannot set GameType({GameType}) while game is running", gameType);
            return;
        }

        GameType = gameType;
    }

    /// <summary>
    ///     Cleanup all use resources of the previous running game
    /// </summary>
    public static void CleanupGame()
    {
        if (GameType == GameType.None)
        {
            Log.Error("Tried to cleanup game, but game is not running");
            return;
        }

        GameType = GameType.None;

        if (Desktop is not null)
        {
            Desktop.Dispose();
            Desktop = new Desktop();
        }

        var vulkanEngine = _container.Resolve<IVulkanEngine>();
        vulkanEngine.WaitForAll();

        var networkHandler = _container.Resolve<INetworkHandler>();
        networkHandler.StopClient();
        networkHandler.StopServer();

        var worldHandler = _container.Resolve<IWorldHandler>();
        worldHandler.DestroyWorlds(GameType.Local);

        var modManager = _container.Resolve<IModManager>();
        modManager.UnloadMods(false);

        ShouldStop = false;
        Tick = 0;
        Timer.Reset();
    }

    private static void CleanUp()
    {
        var modManager = _container.Resolve<IModManager>();
        modManager.UnloadMods(true);

        EnetLibrary.Deinitialize();
        var memoryManager = _container.Resolve<IMemoryManager>();
        memoryManager.Clear();

        if (!HeadlessModeActive)
        {
            var vulkanEngine = _container.Resolve<IVulkanEngine>();
            var asyncFenceAwaiter = _container.Resolve<IAsyncFenceAwaiter>();

            asyncFenceAwaiter.Stop();
            vulkanEngine.Shutdown();
        }

        var allocationHandler = _container.Resolve<IAllocationHandler>();
        allocationHandler.CheckForLeaks(ModState);
        _container.Dispose();
    }

    internal static void RemoveEntitiesByPlayer(ushort player)
    {
        var worldHandler = _container.Resolve<IWorldHandler>();

        foreach (var world in worldHandler.GetWorlds(GameType.Server))
        foreach (var entity in world.EntityManager.GetEntitiesByOwner(player))
            world.EntityManager.EnqueueDestroyEntity(entity);
    }
}