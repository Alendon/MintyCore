using System;
using System.Runtime.InteropServices;
using BulletSharp;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Physics.Native
{
    public partial struct NativeMotionState : IDisposable
    {
        private readonly UnmanagedDisposer _disposer;
        public readonly IntPtr NativePtr;

        internal NativeMotionState(IntPtr nativePtr, UnmanagedDisposer disposer)
        {
            NativePtr = nativePtr;
            _disposer = disposer;
        }

        public DefaultMotionState? GetMotionState()
        {
            var handle = GCHandle.FromIntPtr(UnsafeNativeMethods.btDefaultMotionState_getUserPointer(NativePtr));
            return handle.Target as DefaultMotionState;
        }
        
        /// <summary>
        /// Increase the reference count by one. Remember to dispose(/decrease the reference count) to allow the resources to be freed
        /// </summary>
        public void IncreaseReferenceCount()
        {
            _disposer.IncreaseRefCount();
        }

        /// <summary>
        /// Decrease the reference count of this object by one. If it hits zero the resources will be freed
        /// </summary>
        public void Dispose()
        {
            _disposer.DecreaseRefCount();
        }
    }
}