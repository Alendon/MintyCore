using System;
using System.Runtime.InteropServices;
using static MintyVeldrid.MetalBindings.ObjectiveCRuntime;

namespace MintyVeldrid.MetalBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CAMetalDrawable
    {
        public readonly IntPtr NativePtr;
        public bool IsNull => NativePtr == IntPtr.Zero;
        public MTLTexture texture => objc_msgSend<MTLTexture>(NativePtr, Selectors.texture);
    }
}