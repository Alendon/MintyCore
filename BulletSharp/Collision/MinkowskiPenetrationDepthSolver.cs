using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	public class MinkowskiPenetrationDepthSolver : ConvexPenetrationDepthSolver
	{
		public MinkowskiPenetrationDepthSolver()
		{
			IntPtr native = btMinkowskiPenetrationDepthSolver_new();
			InitializeUserOwned(native);
		}
	}
}
