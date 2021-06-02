using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Utils
{
	[Flags]
	public enum GameType
	{
		Client = 1 << 0,
		Server = 1 << 1,
		Local = Client | Server
	}
}
