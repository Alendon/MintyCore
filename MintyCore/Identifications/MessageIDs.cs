using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
///     <see langword="static" /> class which contains all <see cref="Network.IMessage" /> ids
/// </summary>
public static class MessageIDs
{
    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Network.Messages.ComponentUpdate" />
    /// </summary>
    public static Identification ComponentUpdate { get; set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Network.Messages.AddEntity" />
    /// </summary>
    public static Identification AddEntity { get; set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Network.Messages.RemoveEntity" />
    /// </summary>
    public static Identification RemoveEntity { get; set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Network.Messages.SendEntityData" />
    /// </summary>
    public static Identification SendEntityData { get; set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Network.Messages.PlayerLeft" />
    /// </summary>
    public static Identification PlayerLeft { get; set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Network.Messages.PlayerJoined" />
    /// </summary>
    public static Identification PlayerJoined { get; set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Network.Messages.SyncPlayers" />
    /// </summary>
    public static Identification SyncPlayers { get; set; }
}