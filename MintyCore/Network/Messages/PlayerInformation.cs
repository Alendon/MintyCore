using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
/// Message to send player informations to the server (Name, Id, available mods)
/// </summary>
public partial class PlayerInformation : IMessage
{
    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.PlayerInformation;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

    /// <inheritdoc />
    public ushort Sender { private get; set; }

    /// <summary>
    /// The name of the player
    /// </summary>
    public string PlayerName = String.Empty;
    
    /// <summary>
    /// The global id of the player
    /// </summary>
    public ulong PlayerId;

    /// <summary>
    /// Mods available for the client
    /// </summary>
    public IEnumerable<(string modId, ModVersion version)> AvailableMods =
        Enumerable.Empty<(string modId, ModVersion version)>();


    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(PlayerName);

        writer.Put(AvailableMods.Count());
        foreach (var (modId, modVersion) in AvailableMods)
        {
            writer.Put(modId);
            modVersion.Serialize(writer);
        }
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetULong(out var playerId) || !reader.TryGetString(out var playerName) ||
            !reader.TryGetInt(out var modCount))
        {
            Logger.WriteLog("Failed to deserialize connection setup data", LogImportance.ERROR, "Network");
            return false;
        }

        PlayerId = playerId;
        PlayerName = playerName;

        var mods = new (string modId, ModVersion version)[modCount];

        for (var i = 0; i < modCount; i++)
        {
            if (reader.TryGetString(out mods[i].modId) && ModVersion.Deserialize(reader, out mods[i].version)) continue;

            Logger.WriteLog("Failed to deserialize mod informations", LogImportance.ERROR, "Network");
            return false;
        }

        AvailableMods = mods;

        ProcessReceived();


        return true;
    }

    private void ProcessReceived()
    {
        var server = NetworkHandler.Server;
        Logger.AssertAndThrow(server is not null, "Received Player information message without server?", "Network");
        if (!server.IsPending(Sender))
        {
            return;
        }

        if (!ModManager.ModsCompatible(AvailableMods) ||
            !PlayerHandler.AddPlayer(PlayerName, PlayerId, out var gameId, true))
        {
            server.RejectPending(Sender);
            return;
        }

        server.AcceptPending(Sender, gameId);

        LoadMods loadModsMessage = new()
        {
            Mods = from info in ModManager.GetLoadedMods() select (info.modId, info.modVersion),
            CategoryIDs = RegistryManager.GetCategoryIDs(),
            ModIDs = RegistryManager.GetModIDs(),
            ObjectIDs = RegistryManager.GetObjectIDs()
        };
        loadModsMessage.Send(gameId);


        PlayerConnected playerConnectedMessage = new()
        {
            PlayerGameId = gameId
        };
        playerConnectedMessage.Send(gameId);


        //TODO FUTURE Move this to an internal WorldHandler Method
        if (Engine.ServerWorld is not null)
        {
            foreach (var entity in Engine.ServerWorld.EntityManager.Entities)
            {
                SendEntityData sendEntityData = new()
                {
                    Entity = entity,
                    EntityOwner = Engine.ServerWorld.EntityManager.GetEntityOwner(entity)
                };
                sendEntityData.Send(gameId);
            }
        }

        Logger.WriteLog($"Player {PlayerName} with id: '{PlayerId}' joined the game",
            LogImportance.INFO,
            "Network");

        SyncPlayers syncPlayers = new()
        {
            Players = (from playerId in PlayerHandler.GetConnectedPlayers()
                where playerId != gameId
                select (playerId, PlayerHandler.GetPlayerName(playerId), PlayerHandler.GetPlayerId(playerId))).ToArray()
        };
        syncPlayers.Send(gameId);

        PlayerJoined playerJoined = new()
        {
            GameId = gameId,
            PlayerId = PlayerId,
            PlayerName = PlayerName
        };
        playerJoined.Send(PlayerHandler.GetConnectedPlayers());
    }

    /// <inheritdoc />
    public void Clear()
    {
        PlayerName = String.Empty;
        PlayerId = 0;
        AvailableMods = Enumerable.Empty<(string modId, ModVersion version)>();
    }
}