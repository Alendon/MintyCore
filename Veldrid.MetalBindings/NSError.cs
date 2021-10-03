using System;
using static MintyVeldrid.MetalBindings.ObjectiveCRuntime;

namespace MintyVeldrid.MetalBindings
{
    public struct NSError
    {
        public readonly IntPtr NativePtr;
        public string domain => string_objc_msgSend(NativePtr, "domain");
        public string localizedDescription => string_objc_msgSend(NativePtr, "localizedDescription");
    }
}