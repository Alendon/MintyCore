using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Modding
{
	interface IMod : IDisposable
	{
		ushort ModID { get; }
		string StringIdentifier { get; }

		void Register( ushort modID );
	}
}
