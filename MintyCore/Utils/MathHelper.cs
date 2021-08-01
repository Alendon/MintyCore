using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Utils
{
	/// <summary>
	/// Class for misc math functions
	/// </summary>
	public static class MathHelper
	{
		/// <summary>
		/// Get a number to the next power of 2
		/// </summary>
		public static int CeilPower2(int x )
		{
			if ( x < 2 ) return 1;
			return ( int )Math.Pow( 2, ( int )Math.Log( x - 1, 2 ) + 1 );
		}
	}
}
