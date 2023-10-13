using System;

namespace MintyCore.Utils;

/// <summary>
///     Enum describing the GameType
/// </summary>
[Flags]
public enum GameType
{
    //do not rename members. They are used in the source generation
    
    /// <summary>
    ///     Invalid game state => no game running
    /// </summary>
    Invalid = Constants.InvalidId,

    /// <summary>
    ///     Client Game
    /// </summary>
    Client = 1 << 0,

    /// <summary>
    ///     Server Game
    /// </summary>
    Server = 1 << 1,

    /// <summary>
    ///     Local Game (client and server)
    /// </summary>
    Local = Client | Server
}