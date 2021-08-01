using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Utils
{
	/// <summary>
	/// Enum describing the GameType
	/// </summary>
	[Flags]
	public enum GameType
	{
		/// <summary>
		/// Client Game
		/// </summary>
		Client = 1 << 0,
		/// <summary>
		/// Server Game
		/// </summary>
		Server = 1 << 1,
		/// <summary>
		/// Local Game (client and server)
		/// </summary>
		Local = Client | Server
	}
}
