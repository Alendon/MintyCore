using System.Runtime.InteropServices;

namespace MintyBulletSharp
{
    public static class Native
    {
        public const string Dll = "libbulletc";
        public const CallingConvention Conv = CallingConvention.Cdecl;
    }
}
