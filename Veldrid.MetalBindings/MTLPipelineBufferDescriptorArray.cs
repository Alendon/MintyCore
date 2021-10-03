using System;
using static MintyVeldrid.MetalBindings.ObjectiveCRuntime;

namespace MintyVeldrid.MetalBindings
{
    public struct MTLPipelineBufferDescriptorArray
    {
        public readonly IntPtr NativePtr;

        public MTLPipelineBufferDescriptor this[uint index]
        {
            get
            {
                IntPtr value = IntPtr_objc_msgSend(NativePtr, Selectors.objectAtIndexedSubscript, (UIntPtr)index);
                return new MTLPipelineBufferDescriptor(value);
            }
            set
            {
                objc_msgSend(NativePtr, Selectors.setObjectAtIndexedSubscript, value.NativePtr, (UIntPtr)index);
            }
        }
    }
}