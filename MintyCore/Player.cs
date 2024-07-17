using System;
using JetBrains.Annotations;
using MintyCore.Utils;
using static MintyCore.Utils.Constants;

namespace MintyCore;

/// <summary>
///     Class which represents a player
/// </summary>
[PublicAPI]
public sealed class Player
{
    internal Player(ushort gameId, ulong globalId, string name)
    {
        GameId = gameId;
        GlobalId = globalId;
        Name = name;
        IsConnected = true;
    }

    public static readonly Player InvalidPlayer = new(InvalidId, InvalidId, "Invalid");
    public static readonly Player ServerPlayer = new(ServerId, ServerId, "Server");

    /// <summary>
    ///     Whether or not the player is still connected
    /// </summary>
    public bool IsConnected { get; internal set; }

    /// <summary>
    ///     The game id of the player
    /// </summary>
    public ushort GameId { get; }

    /// <summary>
    ///     The global id of the player
    /// </summary>
    public ulong GlobalId { get; }

    /// <summary>
    ///     The name of the player
    /// </summary>
    public string Name { get; }

    private bool Equals(Player other)
    {
        return GameId == other.GameId && GlobalId == other.GlobalId && Name == other.Name;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is Player other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(GameId, GlobalId, Name);
    }

    /// <summary />
    public static bool operator ==(Player? left, Player? right)
    {
        return Equals(left, right);
    }

    /// <summary />
    public static bool operator !=(Player? left, Player? right)
    {
        return !Equals(left, right);
    }
}