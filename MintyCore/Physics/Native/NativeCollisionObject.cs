using System;
using BulletSharp;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Physics.Native
{
    /// <summary>
    ///     Unmanaged struct to hold a reference to a <see cref="CollisionObject" />
    /// </summary>
    public readonly struct NativeCollisionObject : IDisposable
    {
        private readonly UnmanagedDisposer _disposer;

        /// <summary>
        ///     The pointer of the native <see cref="CollisionObject" />. <seealso cref="CollisionObject.Native" />
        /// </summary>
        public readonly IntPtr NativePtr;

        internal NativeCollisionObject(IntPtr nativePtr, UnmanagedDisposer disposer)
        {
            NativePtr = nativePtr;
            _disposer = disposer;
        }

        /// <summary>
        ///     Get the <see cref="CollisionObject" /> referenced
        /// </summary>
        /// <returns>null if the <see cref="CollisionObject" /> is garbage collected</returns>
        public CollisionObject? GetCollisionObject()
        {
            return CollisionObject.GetManaged(NativePtr);
        }

        /// <summary>
        ///     Increase the reference count by one. Remember to dispose(/decrease the reference count) to allow the resources to
        ///     be freed
        /// </summary>
        public void IncreaseReferenceCount()
        {
            _disposer.IncreaseRefCount();
        }

        /// <summary>
        ///     Decrease the reference count of this object by one. If it hits zero the resources will be freed
        /// </summary>
        public void Dispose()
        {
            _disposer.DecreaseRefCount();
        }
    }
}