using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	public class GjkConvexCast : ConvexCast
	{
		public GjkConvexCast(ConvexShape convexA, ConvexShape convexB, VoronoiSimplexSolver simplexSolver)
		{
			IntPtr native = btGjkConvexCast_new(convexA.Native, convexB.Native, simplexSolver.Native);
			InitializeUserOwned(native);
		}
	}
}
