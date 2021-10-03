using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	public class SphereShape : ConvexInternalShape
	{
		public SphereShape(float radius)
		{
			IntPtr native = btSphereShape_new(radius);
			InitializeCollisionShape(native);
		}

		public void SetUnscaledRadius(float radius)
		{
			btSphereShape_setUnscaledRadius(Native, radius);
		}

		public float Radius => btSphereShape_getRadius(Native);
	}
}
