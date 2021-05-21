using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechardryCoreSharp.Utils
{
	public static class Constants
	{
		//General invalid id for all purposes
		public const int InvalidID = 0;
		//The Server id. This is the lower limit. All values above are also treated as the Server
		public const ushort ServerID = 1 << 15;
	}
}
