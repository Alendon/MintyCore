using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Autofac;
using ENet;
using JetBrains.Annotations;
using MintyCore.AvaloniaIntegration;
using MintyCore.ECS;
using MintyCore.GameStates;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Utils;
using MintyCore.Modding;
using MintyCore.Modding.Implementations;
using MintyCore.Network;
using MintyCore.Utils;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using EnetLibrary = ENet.Library;

namespace MintyCore;

/// <summary>
///     Engine/CoreGame main class
/// </summary>
[PublicAPI]
public class Engine : IEngineConfiguration
{
    /// <inheritdoc />
    public bool HeadlessModeActive { get; private set; }

    public ushort HeadlessPort { get; private set; }

    private readonly List<DirectoryInfo> _additionalModDirectories = new();

    public Thread MainThread { get; } = Thread.CurrentThread;

    /// <summary>
    /// The current mod state
    /// </summary>
    /// <remarks>This is currently not properly used</remarks>
    public ModState ModState { get; } = ModState.RootModsOnly;

    /// <summary>
    /// Indicates whether tests should be active. Meant to replace DEBUG compiler flags
    /// </summary>
    public bool TestingModeActive { get; private set; }
#if DEBUG
        = true;
#endif

    /// <summary>
    ///     The <see cref="GameType" /> of the running instance
    /// </summary>
    public GameType GameType { get; private set; } = GameType.None;


    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyList<string> CommandLineArguments { get; private set; }

    public Identification DefaultGameState { get; set; }
    public Identification DefaultHeadlessGameState { get; set; }

    private IContainer? _rootContainer;
    private IModManager? _modManager;

    private IContainer RootContainer => _rootContainer ?? throw new MintyCoreException("Engine not initialized");
    private IModManager ModManager => _modManager ?? throw new MintyCoreException("Engine not initialized");
    private ILifetimeScope ModsLifetimeScope => ModManager.ModLifetimeScope;


    internal Engine(IReadOnlyList<string> commandLineArguments)
    {
        CommandLineArguments = commandLineArguments;
    }

    internal void Prepare()
    {
        CheckProgramArguments();
        CreateLogger();
    }

    private void BuildRootDiContainer()
    {
        var builder = new ContainerBuilder();

        builder.RegisterInstance(this).As<IEngineConfiguration>().ExternallyOwned();
        builder.RegisterType<ModManager>()
            .As<IModManager>()
            .Named<ModManager>(AutofacHelper.UnsafeSelfName)
            .SingleInstance()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

        _rootContainer = builder.Build();
        _modManager = _rootContainer.Resolve<IModManager>();
    }

    internal void Init()
    {
        Thread.CurrentThread.Name = "MintyCoreMain";

        Log.Information("Initializing Engine");

        BuildRootDiContainer();

        EnetLibrary.Initialize();

        var modManager = RootContainer.Resolve<IModManager>();

        modManager.SearchMods(_additionalModDirectories);
        modManager.LoadRootMods();

        //As the loading of the root mods is done before the game actually starts, we do not know whether a local game or a client game is started
        //The important thing is to not load objects which needs rendering with the headless mode active
        //As a temporary workaround we just set a override game type which the registry manager will use
        var registryGameType = HeadlessModeActive ? GameType.Server : GameType.Local;

        modManager.ProcessRegistry(true, LoadPhase.Pre, registryGameType);

        if (!HeadlessModeActive)
        {
            var vulkanEngine = ModsLifetimeScope.Resolve<IVulkanEngine>();
            var awaiter = ModsLifetimeScope.Resolve<IAsyncFenceAwaiter>();
            var windowHandler = ModsLifetimeScope.Resolve<IWindowHandler>();

            windowHandler.CreateMainWindow();
            vulkanEngine.Setup();
            awaiter.Start();
        }

        modManager.ProcessRegistry(true, LoadPhase.Main, registryGameType);

        //Ui Initialization
        //Must happen after the registry is processed
        if (!HeadlessModeActive)
        {
            var avaloniaController = ModsLifetimeScope.Resolve<IAvaloniaController>();
            avaloniaController.SetupAndRun();
        }

        modManager.ProcessRegistry(true, LoadPhase.Post, registryGameType);

        _overrideGameType = null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void RunGame()
    {
        var stateMachine = ModsLifetimeScope.Resolve<IGameStateMachine>();

        var gameState = HeadlessModeActive ? DefaultHeadlessGameState : DefaultGameState;

        if (gameState == Identification.Invalid)
            throw new MintyCoreException("No default game state set");

        stateMachine.PushGameState(gameState);
        stateMachine.Start();
    }

    private GameType? _overrideGameType;
    internal GameType RegistryGameType => _overrideGameType ?? GameType;

    private void CreateLogger()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithExceptionDetails()
            .Enrich.FromLogContext()
            .WriteTo.File(new CompactJsonFormatter(), $"log/{timestamp}.log", rollOnFileSizeLimit: true,
                flushToDiskInterval: TimeSpan.FromMinutes(1))
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}][{RootNamespace}/{Class}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private void CheckProgramArguments()
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
    public IEnumerable<string> GetCommandLineValues(string key, char? separator = null)
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
    public bool HasCommandLineValue(string key)
    {
        if (!key.StartsWith('-')) key = $"-{key}";
        //check if the key is in the arguments

        return CommandLineArguments.Any(arg => arg == key);
    }

    /// <summary>
    ///     Set the type of the current game
    ///     Only usable if the game is not running
    /// </summary>
    /// <param name="gameType">The type to set to</param>
    public void SetGameType(GameType gameType)
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

    internal void CleanUp()
    {
        EnetLibrary.Deinitialize();
        var memoryManager = ModsLifetimeScope.Resolve<IMemoryManager>();
        memoryManager.Clear();

        if (!HeadlessModeActive)
        {
            var avaloniaController = ModsLifetimeScope.Resolve<IAvaloniaController>();
            avaloniaController.Stop();

            var vulkanEngine = ModsLifetimeScope.Resolve<IVulkanEngine>();
            vulkanEngine.WaitForAll();

            var asyncFenceAwaiter = ModsLifetimeScope.Resolve<IAsyncFenceAwaiter>();
            asyncFenceAwaiter.Stop();
        }

        var allocationHandler = ModsLifetimeScope.Resolve<IAllocationHandler>();
        allocationHandler.CheckForLeaks(ModState);
        
        var modManager = RootContainer.Resolve<IModManager>();
        modManager.UnloadMods(true);

        RootContainer.Dispose();
    }
}