using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	public class Convex2DShape : ConvexShape
	{
		public Convex2DShape(ConvexShape convexChildShape)
		{
			IntPtr native = btConvex2dShape_new(convexChildShape.Native);
			InitializeCollisionShape(native);

			ChildShape = convexChildShape;
		}

		public ConvexShape ChildShape { get; }
	}
}
