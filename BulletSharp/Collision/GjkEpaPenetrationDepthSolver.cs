using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	public class GjkEpaPenetrationDepthSolver : ConvexPenetrationDepthSolver
	{
		public GjkEpaPenetrationDepthSolver()
		{
			IntPtr native = btGjkEpaPenetrationDepthSolver_new();
			InitializeUserOwned(native);
		}
	}
}
