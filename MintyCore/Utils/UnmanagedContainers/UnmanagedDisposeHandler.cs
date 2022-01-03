using System;

namespace MintyCore.Utils.UnmanagedContainers
{
    /// <summary>
    ///     Unmanaged struct to allow easy reference counting of an <see cref="TResource" /> and dispose if all references are
    ///     removed
    /// </summary>
    public readonly unsafe struct UnmanagedDisposer<TResource> where TResource : unmanaged
    {
        private readonly UnsafeUnmanagedDisposer<TResource>* _disposer;

        /// <summary>
        ///     Create a new <see cref="UnmanagedDisposer" />
        /// </summary>
        /// <param name="disposeFunction">The pointer to the static dispose function</param>
        /// <param name="toDispose">The resource to dispose</param>
        public UnmanagedDisposer(delegate*<TResource*, void> disposeFunction, TResource* toDispose)
        {
            if (disposeFunction is null || toDispose is null)
                Logger.WriteLog(
                    $"Tried to create an {nameof(UnmanagedDisposer<TResource>)} with a null dispose function and/or a null dispose ressource",
                    LogImportance.EXCEPTION, "Utils");

            _disposer = UnsafeUnmanagedDisposer<TResource>.CreateDisposer(disposeFunction, toDispose);
        }

        /// <summary>
        ///     Check if the internal disposer is null
        /// </summary>
        public bool Null => _disposer is null;

        /// <summary>
        ///     Increase the reference counter by one. Remember to call <see cref="DecreaseRefCount" /> when the reference is not
        ///     longer needed
        /// </summary>
        public void IncreaseRefCount()
        {
            if (_disposer is null)
                return;
            
            UnsafeUnmanagedDisposer<TResource>.IncreaseRefCount(_disposer);
        }

        /// <summary>
        ///     Decrease the reference counter by one. Calls the dispose function when the counter hits 0
        /// </summary>
        public bool DecreaseRefCount()
        {
            return _disposer is null || UnsafeUnmanagedDisposer<TResource>.DecreaseRefCount(_disposer);
        }
    }


    internal unsafe struct UnsafeUnmanagedDisposer<TResource> where TResource : unmanaged
    {
        private delegate* <TResource*, void> _disposeFunction;
        private TResource* _toDispose;

        private int _referenceCount;


        public static UnsafeUnmanagedDisposer<TResource>* CreateDisposer(delegate*<TResource*, void> disposeFunction,
            TResource* toDispose)
        {
            var ptr = (UnsafeUnmanagedDisposer<TResource>*)AllocationHandler
                .Malloc<UnsafeUnmanagedDisposer<TResource>>();
            *ptr = new UnsafeUnmanagedDisposer<TResource>
                { _disposeFunction = disposeFunction, _referenceCount = 1, _toDispose = toDispose };
            return ptr;
        }

        internal static bool DecreaseRefCount(UnsafeUnmanagedDisposer<TResource>* instance)
        {
            instance->_referenceCount--;
            return CheckDispose(instance);
        }

        internal static void IncreaseRefCount(UnsafeUnmanagedDisposer<TResource>* instance)
        {
            instance->_referenceCount++;
        }

        private static bool CheckDispose(UnsafeUnmanagedDisposer<TResource>* instance)
        {
            if (instance->_referenceCount > 0 || instance->_disposeFunction is null ||
                instance->_toDispose is null) return false;
            instance->_disposeFunction(instance->_toDispose);

            DisposeSelf(instance);
            return true;
        }

        private static void DisposeSelf(UnsafeUnmanagedDisposer<TResource>* instance)
        {
            AllocationHandler.Free(new IntPtr(instance));
        }
    }

    /// <summary>
    ///     Unmanaged struct to allow easy reference counting of an resource and dispose if all references are removed
    /// </summary>
    public readonly unsafe struct UnmanagedDisposer
    {
        private readonly UnsafeUnmanagedDisposer* _disposer;

        /// <summary>
        ///     Create a new <see cref="UnmanagedDisposer" />
        /// </summary>
        /// <param name="disposeFunction">Pointer to a static dispose function</param>
        /// <param name="toDispose">Pointer to the resource to dispose</param>
        public UnmanagedDisposer(delegate*<IntPtr, void> disposeFunction, IntPtr toDispose)
        {
            if (disposeFunction is null || toDispose == IntPtr.Zero)
                Logger.WriteLog(
                    $"Tried to create an {nameof(UnmanagedDisposer)} with a null dispose function and/or a null dispose resource",
                    LogImportance.EXCEPTION, "Utils");

            _disposer = UnsafeUnmanagedDisposer.CreateDisposer(disposeFunction, toDispose);
        }

        /// <summary>
        ///     Check if the internal disposer is null
        /// </summary>
        public bool IsNull => _disposer is null;

        /// <summary>
        ///     Increase the reference counter by one. Remember to call <see cref="DecreaseRefCount" /> when the reference is not
        ///     longer needed
        /// </summary>
        public void IncreaseRefCount()
        {
            if (_disposer is null) return;

            UnsafeUnmanagedDisposer.IncreaseRefCount(_disposer);
        }

        /// <summary>
        ///     Decrease the reference counter by one. Calls the dispose function when the counter hits 0
        /// </summary>
        public void DecreaseRefCount()
        {
            if (_disposer is null) return;

            UnsafeUnmanagedDisposer.DecreaseRefCount(_disposer);
        }
    }

    internal unsafe struct UnsafeUnmanagedDisposer
    {
        private delegate* <IntPtr, void> _disposeFunction;
        private IntPtr _toDispose;

        private int _referenceCount;

        public static UnsafeUnmanagedDisposer* CreateDisposer(delegate*<IntPtr, void> disposeFunction,
            IntPtr toDispose)
        {
            var ptr = (UnsafeUnmanagedDisposer*)AllocationHandler
                .Malloc<UnsafeUnmanagedDisposer>();
            *ptr = new UnsafeUnmanagedDisposer
                { _disposeFunction = disposeFunction, _referenceCount = 1, _toDispose = toDispose };
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
            if (instance->_referenceCount > 0 || instance->_disposeFunction is null ||
                instance->_toDispose == IntPtr.Zero) return;
            instance->_disposeFunction(instance->_toDispose);

            DisposeSelf(instance);
        }

        private static void DisposeSelf(UnsafeUnmanagedDisposer* instance)
        {
            AllocationHandler.Free(new IntPtr(instance));
        }
    }
}