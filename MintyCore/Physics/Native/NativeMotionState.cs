using System;
using System.Runtime.InteropServices;
using BulletSharp;
using BulletSharp.Math;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Physics.Native
{
    public partial struct NativeMotionState
    {
        public readonly UnmanagedDisposer _disposer;
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
    }
}