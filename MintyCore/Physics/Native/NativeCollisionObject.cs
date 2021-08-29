using System;
using BulletSharp;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Physics.Native
{
    public partial struct NativeCollisionObject
    {
        public readonly UnmanagedDisposer _disposer;
        public readonly IntPtr NativePtr;

        internal NativeCollisionObject(IntPtr nativePtr, UnmanagedDisposer disposer)
        {
            NativePtr = nativePtr;
            _disposer = disposer;
        }

        public CollisionObject GetCollisionObject()
        {
            return CollisionObject.GetManaged(NativePtr);
        }
    }
}