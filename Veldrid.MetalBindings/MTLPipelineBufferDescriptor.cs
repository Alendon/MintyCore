using System;
using static MintyVeldrid.MetalBindings.ObjectiveCRuntime;

namespace MintyVeldrid.MetalBindings
{
    public struct MTLPipelineBufferDescriptor
    {
        public readonly IntPtr NativePtr;

        public MTLPipelineBufferDescriptor(IntPtr ptr) => NativePtr = ptr;

        public MTLMutability mutability
        {
            get => (MTLMutability)uint_objc_msgSend(NativePtr, sel_mutability);
            set => objc_msgSend(NativePtr, sel_setMutability, (uint)value);
        }

        private static readonly Selector sel_mutability = "mutability";
        private static readonly Selector sel_setMutability = "setMutability:";
    }
}