using System;
using static MintyVeldrid.MetalBindings.ObjectiveCRuntime;

namespace MintyVeldrid.MetalBindings
{
    public unsafe struct UIScreen
    {
        public readonly IntPtr NativePtr;
        public UIScreen(IntPtr ptr)
        {
            NativePtr = ptr;
        }

        public CGFloat nativeScale => CGFloat_objc_msgSend(NativePtr, "nativeScale");

        public static UIScreen mainScreen
            => objc_msgSend<UIScreen>(new ObjCClass(nameof(UIScreen)), "mainScreen");
    }
}