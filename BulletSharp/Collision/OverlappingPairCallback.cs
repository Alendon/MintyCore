using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	public abstract class OverlappingPairCallback : BulletDisposableObject
	{
		public OverlappingPairCallback(ConstructionInfo info)
		{
		}
		/*
		protected OverlappingPairCallback()
		{
			Native = btOverlappingPairCallbackWrapper_new();
		}
		*/
		public abstract BroadphasePair AddOverlappingPair(BroadphaseProxy proxy0, BroadphaseProxy proxy1);
		public abstract IntPtr RemoveOverlappingPair(BroadphaseProxy proxy0, BroadphaseProxy proxy1, Dispatcher dispatcher);
		public abstract void RemoveOverlappingPairsContainingProxy(BroadphaseProxy proxy0, Dispatcher dispatcher);

		protected override void Dispose(bool disposing)
		{
			if (IsUserOwned)
			{
				btOverlappingPairCallback_delete(Native);
			}
		}
	}
}
