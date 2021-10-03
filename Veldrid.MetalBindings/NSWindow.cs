using System;
using static MintyVeldrid.MetalBindings.ObjectiveCRuntime;

namespace MintyVeldrid.MetalBindings
{
    public unsafe struct NSWindow
    {
        public readonly IntPtr NativePtr;
        public NSWindow(IntPtr ptr)
        {
            NativePtr = ptr;
        }

        public NSView contentView => objc_msgSend<NSView>(NativePtr, "contentView");
    }
}