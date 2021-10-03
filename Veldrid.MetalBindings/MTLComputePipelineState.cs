using System;

namespace MintyVeldrid.MetalBindings
{
    public struct MTLComputePipelineState
    {
        public readonly IntPtr NativePtr;
        public MTLComputePipelineState(IntPtr ptr) => NativePtr = ptr;
        public bool IsNull => NativePtr == IntPtr.Zero;
    }
}