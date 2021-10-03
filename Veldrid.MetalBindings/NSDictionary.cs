using System;

namespace MintyVeldrid.MetalBindings
{
    public struct NSDictionary
    {
        public readonly IntPtr NativePtr;

        public UIntPtr count => ObjectiveCRuntime.UIntPtr_objc_msgSend(NativePtr, "count");
    }
}