using System;
using static MintyVeldrid.MetalBindings.ObjectiveCRuntime;

namespace MintyVeldrid.MetalBindings
{
    public struct MTLVertexAttributeDescriptorArray
    {
        public readonly IntPtr NativePtr;

        public MTLVertexAttributeDescriptor this[uint index]
        {
            get
            {
                IntPtr value = IntPtr_objc_msgSend(NativePtr, Selectors.objectAtIndexedSubscript, index);
                return new MTLVertexAttributeDescriptor(value);
            }
            set => objc_msgSend(NativePtr, Selectors.setObjectAtIndexedSubscript, value.NativePtr, index);
        }
    }
}