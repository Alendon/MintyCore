using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	public class DantzigSolver : MlcpSolverInterface
	{
		public DantzigSolver()
		{
			IntPtr native = btDantzigSolver_new();
			InitializeUserOwned(native);
		}
	}
}
