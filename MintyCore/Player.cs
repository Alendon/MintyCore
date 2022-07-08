using JetBrains.Annotations;

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
}