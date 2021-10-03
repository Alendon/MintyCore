using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp.SoftBody
{
	public class DeformableBodySolver : SoftBodySolver
	{
		public DeformableBodySolver()
		{
			IntPtr native = btDeformableBodySolver_new();
			InitializeUserOwned(native);
		}
	}
}
