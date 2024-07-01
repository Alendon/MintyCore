using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Network.Messages;

/// <summary>
/// Message to send player informations to the server (Name, Id, available mods)
/// </summary>
[RegisterMessage("player_information")]
public class PlayerInformation : Message
{
    /// <inheritdoc />
    public override bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public override Identification MessageId => MessageIDs.PlayerInformation;

    /// <inheritdoc />
    public override DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;


    /// <summary>
    /// The name of the player
    /// </summary>
    public string PlayerName = string.Empty;

    /// <summary>
    /// The global id of the player
    /// </summary>
    public ulong PlayerId;

    /// <summary>
    /// Mods available for the client
    /// </summary>
    public IEnumerable<(string modId, Version version)> AvailableMods =
        [];

    /// <summary/>
    public required IModManager ModManager { private get; init; }

    private IRegistryManager RegistryManager => ModManager.RegistryManager;

    /// <summary/>
    public required IPlayerHandler PlayerHandler { private get; init; }

    /// <summary/>
    public required IWorldHandler WorldHandler { private get; init; }


    /// <inheritdoc />
    public override void Serialize(DataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(PlayerName);

        writer.Put(AvailableMods.Count());
        foreach (var (modId, modVersion) in AvailableMods)
        {
            writer.Put(modId);
            writer.Put(modVersion);
        }
    }

    /// <inheritdoc />
    public override bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetULong(out var playerId) || !reader.TryGetString(out var playerName) ||
            !reader.TryGetInt(out var modCount))
        {
            Log.Error("Failed to deserialize connection setup data");
            return false;
        }

        PlayerId = playerId;
        PlayerName = playerName;

        var mods = new (string modId, Version version)[modCount];

        for (var i = 0; i < modCount; i++)
        {
            if (reader.TryGetString(out mods[i].modId) && reader.TryGetVersion(out var version))
            {
                mods[i].version = version;
                continue;
            }

            Log.Error("Failed to deserialize mod information's");
            return false;
        }

        AvailableMods = mods;

        ProcessReceived();


        return true;
    }

    private void ProcessReceived()
    {
        var server = NetworkHandler.Server;
        if (server is null) throw new MintyCoreException("Received Player information message without server?");
        if (!server.IsPending(Sender)) return;

        if (!ModManager.ModsCompatible(AvailableMods) ||
            !PlayerHandler.AddPlayer(PlayerName, PlayerId, out var player, true))
        {
            server.RejectPending(Sender);
            return;
        }

        server.AcceptPending(Sender, player);

        var loadModsMessage = NetworkHandler.CreateMessage<LoadMods>();

        loadModsMessage.Mods = from info in ModManager.GetLoadedMods() select (info.modId, info.modVersion);
        loadModsMessage.CategoryIDs = RegistryManager.GetCategoryIDs();
        loadModsMessage.ModIDs = RegistryManager.GetModIDs();
        loadModsMessage.ObjectIDs = RegistryManager.GetObjectIDs();

        loadModsMessage.Send(player);


        var playerConnectedMessage = NetworkHandler.CreateMessage<PlayerConnected>();
        playerConnectedMessage.PlayerGameId = player.GameId;

        playerConnectedMessage.Send(player);


        WorldHandler.SendEntitiesToPlayer(player);

        Log.Information("Player {PlayerName} with id: '{PlayerId}' joined the game", PlayerName, PlayerId);

        var syncPlayers = NetworkHandler.CreateMessage<SyncPlayers>();
        syncPlayers.Players = PlayerHandler.GetConnectedPlayers().Except([player]).ToArray();

        syncPlayers.Send(player);

        var playerJoined = NetworkHandler.CreateMessage<PlayerJoined>();
        playerJoined.GameId = player.GameId;
        playerJoined.PlayerId = PlayerId;
        playerJoined.PlayerName = PlayerName;

        playerJoined.Send(PlayerHandler.GetConnectedPlayers());
    }

    /// <inheritdoc />
    public override void Clear()
    {
        PlayerName = string.Empty;
        PlayerId = 0;
        AvailableMods = [];
    }
}