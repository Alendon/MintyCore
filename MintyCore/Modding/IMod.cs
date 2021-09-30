using System;
using MintyCore.Utils;

namespace MintyCore.Modding
{
    public interface IMod : IDisposable
    {
        ushort ModId { get; set; }
        string StringIdentifier { get; }
        string ModDescription { get; }
        string ModName { get; }
        ModVersion ModVersion { get; }
        ModDependency[] ModDependencies { get; }
        GameType ExecutionSide { get; }

        void PreLoad();
        void Load();
        void PostLoad();
        
        void Unload();
    }
}