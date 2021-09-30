using System;

namespace MintyCore.Utils
{
	/// <summary>
	///     Enum describing the GameType
	/// </summary>
	[Flags]
    public enum GameType
    {
	    INVALID = Constants.InvalidId,
	    
	    /// <summary>
	    ///     Client Game
	    /// </summary>
	    CLIENT = 1 << 0,

	    /// <summary>
	    ///     Server Game
	    /// </summary>
	    SERVER = 1 << 1,

	    /// <summary>
	    ///     Local Game (client and server)
	    /// </summary>
	    LOCAL = CLIENT | SERVER
    }
}