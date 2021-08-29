using System;
using BulletSharp;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Physics.Native
{
    public partial struct NativeCollisionShape
    {
        public readonly UnmanagedDisposer _disposer;
        public readonly IntPtr NativePtr;

        internal NativeCollisionShape(IntPtr nativePtr, UnmanagedDisposer disposer)
        {
            NativePtr = nativePtr;
            _disposer = disposer;
        }

        public CollisionShape GetCollisionShape()
        {
            return CollisionShape.GetManaged(NativePtr);
        }
    }
}