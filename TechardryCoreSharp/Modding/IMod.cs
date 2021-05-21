using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechardryCoreSharp.Modding
{
	interface IMod
	{
		ushort ModID { get; }
		string StringIdentifier { get; }

		void Register( ushort modID );
	}
}
