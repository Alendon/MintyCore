using System;
using static MintyBulletSharp.UnsafeNativeMethods;
namespace MintyBulletSharp
{
	public class EmptyShape : ConcaveShape
	{
		public EmptyShape()
		{
			IntPtr native = btEmptyShape_new();
			InitializeCollisionShape(native);
		}
	}
}
