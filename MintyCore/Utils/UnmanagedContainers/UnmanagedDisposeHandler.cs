using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Utils.UnmanagedContainers
{
	public unsafe struct UnmanagedDisposer<TRessource> where TRessource : unmanaged
	{
		private delegate* <TRessource*, void> _disposeFunction;
		private readonly TRessource* _toDispose;
		private byte _selfDispose;
		private UnmanagedDisposer<TRessource>* self;

		public int ReferenceCount { get; private set; }

		public UnmanagedDisposer(delegate*<TRessource*, void> disposeFunction, TRessource* toDispose)
		{
			if (disposeFunction is null || toDispose is null)
			{
				Logger.WriteLog($"Tried to create an {nameof(UnmanagedDisposer<TRessource>)} with a null dispose function and/or a null dispose ressource", LogImportance.EXCEPTION, "Utils");
			}

			_disposeFunction = disposeFunction;
			ReferenceCount = 1;
			_toDispose = toDispose;
			_selfDispose = 0;
			self = null;
		}

		public static UnmanagedDisposer<TRessource>* CreateDisposer(delegate*<TRessource*, void> disposeFunction, TRessource* toDispose)
		{
			var ptr = (UnmanagedDisposer<TRessource>*)AllocationHandler.Malloc<UnmanagedDisposer<TRessource>>();
			*ptr = new UnmanagedDisposer<TRessource>(disposeFunction, toDispose);
			ptr->_selfDispose = 1;
			ptr->self = ptr;
			return ptr;
		}

		public void DecreaseRefCount()
		{
			ReferenceCount--;
			CheckDispose();
		}

		public void IncreaseRefCount()
		{
			ReferenceCount++;
		}

		private void CheckDispose()
		{
			if(ReferenceCount <= 0 && _disposeFunction is not null && _toDispose is not null )
			{
				_disposeFunction(_toDispose);
				if(_selfDispose != 0 && self is not null)
				{
					DisposeSelf(self);
				}
			}
		}

		private static void DisposeSelf(UnmanagedDisposer<TRessource>* instance)
		{
			AllocationHandler.Free(new System.IntPtr(instance));
		}
	}
}
