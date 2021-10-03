using System;
using static MintyVeldrid.MetalBindings.ObjectiveCRuntime;

namespace MintyVeldrid.MetalBindings
{
    public struct MTLVertexBufferLayoutDescriptorArray
    {
        public readonly IntPtr NativePtr;

        public MTLVertexBufferLayoutDescriptor this[uint index]
        {
            get
            {
                IntPtr value = IntPtr_objc_msgSend(NativePtr, Selectors.objectAtIndexedSubscript, index);
                return new MTLVertexBufferLayoutDescriptor(value);
            }
            set => objc_msgSend(NativePtr, Selectors.setObjectAtIndexedSubscript, value.NativePtr, index);
        }
    }
}