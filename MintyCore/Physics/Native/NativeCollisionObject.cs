using System;
using BulletSharp;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Physics.Native
{
    public partial struct NativeCollisionObject : IDisposable
    {
        private readonly UnmanagedDisposer _disposer;
        public readonly IntPtr NativePtr;

        internal NativeCollisionObject(IntPtr nativePtr, UnmanagedDisposer disposer)
        {
            NativePtr = nativePtr;
            _disposer = disposer;
        }

        public CollisionObject? GetCollisionObject()
        {
            return CollisionObject.GetManaged(NativePtr);
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