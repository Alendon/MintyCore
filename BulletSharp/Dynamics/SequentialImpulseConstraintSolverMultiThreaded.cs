using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	public class SequentialImpulseConstraintSolverMultiThreaded : SequentialImpulseConstraintSolver
	{
		public SequentialImpulseConstraintSolverMultiThreaded()
			: base(ConstructionInfo.Null)
		{
			IntPtr native = btSequentialImpulseConstraintSolverMt_new();
			InitializeUserOwned(native);
		}
	}
}
