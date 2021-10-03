using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	public class SphereTriangleCollisionAlgorithm : ActivatingCollisionAlgorithm
	{
		public class CreateFunc : CollisionAlgorithmCreateFunc
		{
			public CreateFunc(IntPtr native, BulletObject owner)
				: base(ConstructionInfo.Null)
			{
				InitializeSubObject(native, owner);
			}

			public CreateFunc()
				: base(ConstructionInfo.Null)
			{
				IntPtr native = btSphereTriangleCollisionAlgorithm_CreateFunc_new();
				InitializeUserOwned(native);
			}

			public override CollisionAlgorithm CreateCollisionAlgorithm(CollisionAlgorithmConstructionInfo __unnamed0, CollisionObjectWrapper body0Wrap, CollisionObjectWrapper body1Wrap)
			{
				return new SphereTriangleCollisionAlgorithm(btCollisionAlgorithmCreateFunc_CreateCollisionAlgorithm(
					Native, __unnamed0.Native, body0Wrap.Native, body1Wrap.Native), __unnamed0.Dispatcher);
			}
		}

		public SphereTriangleCollisionAlgorithm(IntPtr native, BulletObject owner)
		{
			InitializeSubObject(native, owner);
		}

		public SphereTriangleCollisionAlgorithm(PersistentManifold mf, CollisionAlgorithmConstructionInfo ci,
			CollisionObjectWrapper body0Wrap, CollisionObjectWrapper body1Wrap, bool swapped)
		{
			IntPtr native = btSphereTriangleCollisionAlgorithm_new(mf.Native, ci.Native,
				body0Wrap.Native, body1Wrap.Native, swapped);
			InitializeUserOwned(native);
		}

		public SphereTriangleCollisionAlgorithm(CollisionAlgorithmConstructionInfo ci)
		{
			IntPtr native = btSphereTriangleCollisionAlgorithm_new2(ci.Native);
			InitializeUserOwned(native);
		}
	}
}
