using System;
using static MintyVeldrid.MetalBindings.ObjectiveCRuntime;

namespace MintyVeldrid.MetalBindings
{
    public struct CALayer
    {
        public readonly IntPtr NativePtr;
        public CALayer(IntPtr ptr) => NativePtr = ptr;

        public void addSublayer(IntPtr layer)
        {
            objc_msgSend(NativePtr, "addSublayer:", layer);
        }
    }
}