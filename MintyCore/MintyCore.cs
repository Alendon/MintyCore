using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using BulletSharp;
using ENet;
using ImGuiNET;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Network;
using MintyCore.Network.Messages;
using MintyCore.Render;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore
{
    /// <summary>
    ///     Engine/CoreGame main class
    /// </summary>
    public static class MintyCore
    {
        private static readonly Stopwatch _tickTimeWatch = new();

        private static readonly MintyCoreMod _mod = new();

        internal static RenderModeEnum RenderMode = RenderModeEnum.NORMAL;

        public static World? ServerWorld;
        public static World? ClientWorld;

        public static Server? Server;
        public static Client? Client;

        internal static bool GameLoopRunning;
        private static Stopwatch _tick;
        private static Stopwatch _render;
        private static double _accumulatedMillis;
        private static Random _rnd;
        private static double _msDuration;

        private static RenderAble _renderAble;

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

        public const int MaxTickCount = 1_000_000_000;

        private static void Main(string[] args)
        {
            Init();
            MainMenu();
            CleanUp();
        }


        private static void Init()
        {
            Thread.CurrentThread.Name = "MintyCoreMain";

            Logger.InitializeLog();


            Library.Initialize();
            Window = new Window();

            VulkanEngine.Setup();

            ModManager.SearchMods();
        }

        public delegate void DrawUI();

        public static event DrawUI OnDrawGameUI = delegate { };

        private static void MainMenu()
        {
            var bufferTargetAddress = "localhost";
            var bufferPort = "5665";
            ushort port = 5665;

            var playerIdInput = "0";
            var playerNameInput = "Player";

            Dictionary<string, List<ModInfo>> modsWithId = new();
            Dictionary<string, bool> modsActive = new();
            Dictionary<string, bool> versionSelectActive = new();
            Dictionary<string, List<string>> modsVersionsList = new();
            Dictionary<string, string[]> modsVersionsArray = new();
            Dictionary<string, int> modsVersionIndex = new();

            foreach (var modInfo in ModManager.GetAvailableMods())
            {
                if (!modsWithId.ContainsKey(modInfo.ModId))
                {
                    modsWithId.Add(modInfo.ModId, new List<ModInfo>());
                    modsActive.Add(modInfo.ModId, false);
                    versionSelectActive.Add(modInfo.ModId, false);
                    modsVersionIndex.Add(modInfo.ModId, 0);
                    modsVersionsList.Add(modInfo.ModId, new());
                }

                modsWithId[modInfo.ModId].Add(modInfo);
                modsVersionsList[modInfo.ModId].Add(modInfo.ModVersion.ToString());
            }

            foreach (var (modId, versions) in modsVersionsList)
            {
                modsVersionsArray.Add(modId, versions.ToArray());
            }

            while (Window is not null && Window.Exists)
            {
                SetDeltaTime();
                var snapshot = Window.PollEvents();
                VulkanEngine.PrepareDraw(snapshot);

                var connectToServer = false;
                var createServer = false;
                var localGame = false;

                if (ImGui.Begin("\"Main Menu\""))
                {
                    ImGui.InputText("ServerAddress", ref bufferTargetAddress, 50);

                    var lastPort = bufferPort;
                    ImGui.InputText("Port", ref bufferPort, 5);
                    if (!ushort.TryParse(bufferPort, out port))
                    {
                        port = ushort.Parse(lastPort);
                        bufferPort = lastPort;
                    }

                    ImGui.InputText("Name", ref playerNameInput, 50);
                    LocalPlayerName = playerNameInput;

                    ImGui.InputText("ID", ref playerIdInput, 25);
                    if (ulong.TryParse(playerIdInput, out var id))
                    {
                        LocalPlayerId = id;
                    }
                    else
                    {
                        playerIdInput = LocalPlayerId.ToString();
                    }


                    if (ImGui.Button("Connect to Server")) connectToServer = true;

                    if (ImGui.Button("Create Server")) createServer = true;

                    if (ImGui.Button("Local Game")) localGame = true;
                    ImGui.End();
                }


                if (ImGui.Begin("Mod Selection"))
                {
                    foreach (var (mod, active) in modsActive)
                    {
                        var modVersionIndex = modsVersionIndex[mod];
                        var modInfo = modsWithId[mod][modVersionIndex];

                        var modActive = active;
                        ImGui.Checkbox($"{modInfo.ModName} - {modInfo.ExecutionSide}###{modInfo.ModId}", ref modActive);
                        if (modInfo.ModId.Equals("minty_core")) modActive = true;

                        if (modActive && modInfo.ModDependencies.Length != 0)
                        {
                            foreach (var dependency in modInfo.ModDependencies)
                            {
                                modsActive[dependency.StringIdentifier] = true;
                            }
                        }

                        modsVersionIndex[mod] = modVersionIndex;
                        modsActive[mod] = modActive;
                    }

                    ImGui.End();
                }

                VulkanEngine.BeginDraw();
                VulkanEngine.EndDraw();

                //Just check which game type we will start
                GameType = createServer ? GameType.SERVER : GameType;
                GameType = connectToServer ? GameType.CLIENT : GameType;
                GameType = localGame ? GameType.LOCAL : GameType;

                if (GameType == GameType.INVALID) continue;
                
                
                ShouldStop = false;
                
                if (GameType.HasFlag(GameType.SERVER))
                {
                    var modsToLoad = from mods in modsActive
                        where mods.Value
                        select modsWithId[mods.Key].First();
                    
                    ModManager.LoadMods(modsToLoad);
                    
                    LoadWorld();

                    Server = new Server();
                    Server.Start(port, 16);
                }
                
                if (GameType.HasFlag(GameType.CLIENT))
                {
                    Client = new Client();
                    Client.Connect(bufferTargetAddress, port);
                }

                while (LocalPlayerGameId == Constants.InvalidId)
                {
                    Server?.Update();
                    Client?.Update();
                }
                
                GameLoop();
                
                ServerWorld?.Dispose();
                ClientWorld?.Dispose();

                ServerWorld = null;
                ClientWorld = null;

                GameType = GameType.INVALID;


            }
        }

        internal static bool ShouldStop;
        internal static bool Stop => ShouldStop || Window is not null && !Window.Exists;

        internal static void LoadWorld()
        {
            ServerWorld = new World(true);
            ServerWorld.SetupTick();

            var materials = new UnmanagedArray<GCHandle>(1)
            {
                [0] = MaterialHandler.GetMaterialHandle(MaterialIDs.Color)
            };

            _renderAble = new RenderAble();
            _renderAble.SetMesh(MeshIDs.Cube);
            _renderAble.SetMaterials(materials);

            var cubePos = new Position { Value = new Vector3(0, -3, 0) };
            Transform transform = new();
            transform.PopulateWithDefaultValues();

            var plane = ServerWorld.EntityManager.CreateEntity(ArchetypeIDs.Mesh);
            var scale = new Scale
            {
                Value = new Vector3(10, 0.1f, 10)
            };

            ServerWorld.EntityManager.SetComponent(plane, _renderAble);
            ServerWorld.EntityManager.SetComponent(plane, scale);
            ServerWorld.EntityManager.SetComponent(plane, cubePos);

            materials.DecreaseRefCount();
            _renderAble.DecreaseRefCount();
        }

        internal static void GameLoop()
        {
            var serverUpdateData = new ComponentUpdate.ComponentData();
            var clientUpdateData = new ComponentUpdate.ComponentData();
            var serverUpdateDic = serverUpdateData.components;
            var clientUpdateDic = clientUpdateData.components;


            while (Stop == false)
            {
                SetDeltaTime();

                var snapshot = Window.PollEvents();

                VulkanEngine.PrepareDraw(snapshot);
                OnDrawGameUI.Invoke();
                VulkanEngine.BeginDraw();

                ServerWorld?.Tick();
                ClientWorld?.Tick();

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
                            ;
                            if (!serverUpdateDic.ContainsKey(component.Entity))
                                serverUpdateDic.Add(component.Entity, new());
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
                                clientUpdateDic.Add(component.Entity, new());
                            clientUpdateDic[component.Entity].Add((component.ComponentId, component.ComponentPtr));
                        }
                    }
                }

                Server?.MessageHandler.SendMessage(MessageIDs.ComponentUpdate, serverUpdateData);
                Client?.MessageHandler.SendMessage(MessageIDs.ComponentUpdate, clientUpdateData);

                foreach (var updateValues in clientUpdateDic.Values)
                {
                    updateValues.Clear();
                }

                foreach (var updateValues in serverUpdateDic.Values)
                {
                    updateValues.Clear();
                }

                Server?.Update();
                if (Client is not null)
                {
                    Client.Update();
                    Server?.Update();
                }


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

        internal static void NextRenderMode()
        {
            var numRenderMode = (int)RenderMode;
            numRenderMode %= 3;
            numRenderMode++;
            RenderMode = (RenderModeEnum)numRenderMode;
        }

        internal static void SpawnPlayer(ushort playerID)
        {
            if (ServerWorld is null) return;

            var materials = new UnmanagedArray<GCHandle>(1);
            materials[0] = MaterialHandler.GetMaterialHandle(MaterialIDs.Color);

            _renderAble = new RenderAble();
            _renderAble.SetMesh(MeshIDs.Capsule);
            _renderAble.SetMaterials(materials);

            var playerEntity = ServerWorld.EntityManager.CreateEntity(ArchetypeIDs.Player, playerID);

            var playerPos = new Position { Value = new Vector3(0, 0, 5) };
            ServerWorld.EntityManager.SetComponent(playerEntity, playerPos);
            ServerWorld.EntityManager.SetComponent(playerEntity, _renderAble);

            materials.DecreaseRefCount();
            _renderAble.DecreaseRefCount();
        }

        private static void CleanUp()
        {
            var tracker = BulletObjectTracker.Current;
            if (tracker.GetUserOwnedObjects().Count != 0)
                Logger.WriteLog($"{tracker.GetUserOwnedObjects().Count} BulletObjects were not disposed",
                    LogImportance.WARNING, "Physics");
            else
                Logger.WriteLog("All BulletObjects were disposed", LogImportance.INFO, "Physics");

            ModManager.UnloadMods();

            Library.Deinitialize();
            VulkanEngine.Stop();
            AllocationHandler.CheckUnFreed();
        }

        [Flags]
        internal enum RenderModeEnum
        {
            NORMAL = 1,
            WIREFRAME = 2
        }

        internal static Dictionary<ushort, ulong> _playerIDs = new();
        internal static Dictionary<ushort, string> _playerNames = new();

        public static ushort LocalPlayerGameId { get; internal set; } = Constants.InvalidId;
        public static ulong LocalPlayerId { get; internal set; } = Constants.InvalidId;
        public static string LocalPlayerName { get; internal set; } = "Player";

        public static void RemovePlayer(ushort playerId)
        {
            _playerIDs.Remove(playerId);
            _playerNames.Remove(playerId);
        }

        internal static void RemovePlayerEntities(ushort playerId)
        {
            if (ServerWorld is not null)
            {
                foreach (Entity entity in ServerWorld.EntityManager.GetEntitiesByOwner(playerId))
                {
                    ServerWorld.EntityManager.DestroyEntity(entity);
                }
            }
        }

        public static void AddPlayer(ushort gameId, string playerName, ulong playerId)
        {
            if (_playerIDs.ContainsKey(gameId)) return;

            _playerIDs.Add(gameId, playerId);
            _playerNames.Add(gameId, playerName);
        }

        public static bool AddPlayer(string playerName, ulong playerId, out ushort id)
        {
            //TODO implement
            id = Constants.ServerId + 1;
            while (_playerIDs.ContainsKey(id)) id++;

            _playerIDs.Add(id, playerId);
            _playerNames.Add(id, playerName);

            return true;
        }

        public static void CreatePlayerWorld()
        {
            ClientWorld = new World(false);
            ClientWorld.SetupTick();
        }
    }
}