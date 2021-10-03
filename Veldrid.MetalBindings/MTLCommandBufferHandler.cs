using System;
using System.Runtime.InteropServices;

namespace MintyVeldrid.MetalBindings
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MTLCommandBufferHandler(IntPtr block, MTLCommandBuffer buffer);
}