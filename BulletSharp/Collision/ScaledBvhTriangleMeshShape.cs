using MintyBulletSharp.Math;
using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	public class ScaledBvhTriangleMeshShape : ConcaveShape
	{
		public ScaledBvhTriangleMeshShape(BvhTriangleMeshShape childShape, Vector3 localScaling)
		{
			IntPtr native = btScaledBvhTriangleMeshShape_new(childShape.Native, ref localScaling);
			InitializeCollisionShape(native);

			ChildShape = childShape;
		}

		public BvhTriangleMeshShape ChildShape { get; }
	}
}
