using System;

namespace MintyVeldrid.MetalBindings
{
    public struct MTLRenderPipelineState
    {
        public readonly IntPtr NativePtr;
        public MTLRenderPipelineState(IntPtr ptr) => NativePtr = ptr;
    }
}