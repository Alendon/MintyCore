using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp.SoftBody
{
	public abstract class DeformableLagrangianForce : BulletDisposableObject
	{
		protected DeformableLagrangianForce()
		{
		}

		protected override void Dispose(bool disposing)
		{
			btDeformableLagrangianForce_delete(Native);
		}
	}
}
