using System;

namespace MintyCore.Utils.UnmanagedContainers
{
    public unsafe struct UnmanagedDisposer<TRessource> where TRessource : unmanaged
    {
        private UnsafeUnmanagedDisposer<TRessource>* _disposer;

        public UnmanagedDisposer(delegate*<TRessource*, void> disposeFunction, TRessource* toDispose)
        {
            if (disposeFunction is null || toDispose is null)
            {
                Logger.WriteLog(
                    $"Tried to create an {nameof(UnmanagedDisposer<TRessource>)} with a null dispose function and/or a null dispose ressource",
                    LogImportance.EXCEPTION, "Utils");
            }

            _disposer = UnsafeUnmanagedDisposer<TRessource>.CreateDisposer(disposeFunction, toDispose);
        }

        public void IncreaseRefCount()
        {
#if DEBUG
            if (_disposer is null)
            {
                Logger.WriteLog("Tried to increase the reference count with an uninitialized disposer",
                    LogImportance.EXCEPTION, "Utils");
            }
#endif
            UnsafeUnmanagedDisposer<TRessource>.IncreaseRefCount(_disposer);
        }

        public void DecreaseRefCount()
        {
#if DEBUG
            if (_disposer is null)
            {
                Logger.WriteLog("Tried to decrease the reference count with an uninitialized disposer",
                    LogImportance.EXCEPTION, "Utils");
            }
#endif
            UnsafeUnmanagedDisposer<TRessource>.DecreaseRefCount(_disposer);
        }
    }

    public unsafe struct UnsafeUnmanagedDisposer<TRessource> where TRessource : unmanaged
    {
        private delegate* <TRessource*, void> _disposeFunction;
        private TRessource* _toDispose;

        private int _referenceCount;


        public static UnsafeUnmanagedDisposer<TRessource>* CreateDisposer(delegate*<TRessource*, void> disposeFunction,
            TRessource* toDispose)
        {
            var ptr = (UnsafeUnmanagedDisposer<TRessource>*)AllocationHandler
                .Malloc<UnsafeUnmanagedDisposer<TRessource>>();
            *ptr = new() { _disposeFunction = disposeFunction, _referenceCount = 1, _toDispose = toDispose};
            return ptr;
        }

        internal static void DecreaseRefCount(UnsafeUnmanagedDisposer<TRessource>* instance)
        {
            instance->_referenceCount--;
            CheckDispose(instance);
        }

        internal static void IncreaseRefCount(UnsafeUnmanagedDisposer<TRessource>* instance)
        {
            instance->_referenceCount++;
        }

        private static void CheckDispose(UnsafeUnmanagedDisposer<TRessource>* instance)
        {
            if (instance->_referenceCount <= 0 && instance->_disposeFunction is not null && instance->_toDispose is not null)
            {
                instance->_disposeFunction(instance->_toDispose);

                DisposeSelf(instance);
            }
        }

        private static void DisposeSelf(UnsafeUnmanagedDisposer<TRessource>* instance)
        {
            AllocationHandler.Free(new System.IntPtr(instance));
        }
    }

    public unsafe struct UnmanagedDisposer
    {
        private UnsafeUnmanagedDisposer* _disposer;

        public UnmanagedDisposer(delegate*<IntPtr, void> disposeFunction, IntPtr toDispose)
        {
            if (disposeFunction is null || toDispose == IntPtr.Zero)
            {
                Logger.WriteLog(
                    $"Tried to create an {nameof(UnmanagedDisposer)} with a null dispose function and/or a null dispose ressource",
                    LogImportance.EXCEPTION, "Utils");
            }

            _disposer = UnsafeUnmanagedDisposer.CreateDisposer(disposeFunction, toDispose);
        }

        public bool IsNull => _disposer is null;
        
        public readonly void IncreaseRefCount()
        {

            if (_disposer is null)
            {
                return;
            }

            UnsafeUnmanagedDisposer.IncreaseRefCount(_disposer);
        }

        public readonly void DecreaseRefCount()
        {

            if (_disposer is null)
            {
                return;
            }

            UnsafeUnmanagedDisposer.DecreaseRefCount(_disposer);
        }
    }

    public unsafe struct UnsafeUnmanagedDisposer
    {
        private delegate* <IntPtr, void> _disposeFunction;
        private IntPtr _toDispose;

        private int _referenceCount;
        
        public static UnsafeUnmanagedDisposer* CreateDisposer(delegate*<IntPtr, void> disposeFunction,
            IntPtr toDispose)
        {
            var ptr = (UnsafeUnmanagedDisposer*)AllocationHandler
                .Malloc<UnsafeUnmanagedDisposer>();
            *ptr = new() { _disposeFunction = disposeFunction, _referenceCount = 1, _toDispose = toDispose};
            return ptr;
        }

        internal static void DecreaseRefCount(UnsafeUnmanagedDisposer* instance)
        {
            instance->_referenceCount--;
            CheckDispose(instance);
        }

        internal static void IncreaseRefCount(UnsafeUnmanagedDisposer* instance)
        {
            instance->_referenceCount++;
        }

        private static void CheckDispose(UnsafeUnmanagedDisposer* instance)
        {
            if (instance->_referenceCount <= 0 && instance->_disposeFunction is not null && instance->_toDispose != IntPtr.Zero)
            {
                instance->_disposeFunction(instance->_toDispose);

                DisposeSelf(instance);
            }
        }

        private static void DisposeSelf(UnsafeUnmanagedDisposer* instance)
        {
            AllocationHandler.Free(new System.IntPtr(instance));
        }
    }
}