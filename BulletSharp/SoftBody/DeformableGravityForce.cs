using MintyBulletSharp.Math;
using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp.SoftBody
{
	public class DeformableGravityForce : DeformableLagrangianForce
	{
		public DeformableGravityForce(Vector3 gravity)
		{
			IntPtr native = btDeformableGravityForce_new(ref gravity);
			InitializeUserOwned(native);
		}
	}
}
