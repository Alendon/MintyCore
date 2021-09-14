using System;

namespace MintyCore.Modding
{
    internal interface IMod : IDisposable
    {
        ushort ModId { get; }
        string StringIdentifier { get; }

        void Load(ushort modId);
        void Unload();
    }
}