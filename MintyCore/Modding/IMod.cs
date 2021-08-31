using System;

namespace MintyCore.Modding
{
    internal interface IMod : IDisposable
    {
        ushort ModId { get; }
        string StringIdentifier { get; }

        void Register(ushort modId);
    }
}