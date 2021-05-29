using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechardryCoreSharp.Utils
{
	public static class MathHelper
	{
		public static int CeilPower2(int x )
		{
			if ( x < 2 ) return 1;
			return ( int )System.Math.Pow( 2, ( int )System.Math.Log( x - 1, 2 ) + 1 );
		}
	}
}
