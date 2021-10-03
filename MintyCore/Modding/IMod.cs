using System;
using MintyCore.Utils;

namespace MintyCore.Modding
{
    /// <summary>
    /// Base interface for all mod implementations
    /// </summary>
    public interface IMod : IDisposable
    {
        /// <summary>
        /// Set accessor to pass the numeric mod id
        /// </summary>
        ushort ModId { set; }
        
        /// <summary>
        /// String representation of the mod id
        /// </summary>
        string StringIdentifier { get; }
        
        /// <summary>
        /// Description of the mod
        /// </summary>
        string ModDescription { get; }
        
        /// <summary>
        /// Name of the mod
        /// </summary>
        string ModName { get; }
        
        /// <summary>
        /// Version of the mod
        /// </summary>
        ModVersion ModVersion { get; }
        
        /// <summary>
        /// Dependencies the mod have, may be empty but not null
        /// </summary>
        ModDependency[] ModDependencies { get; }
        
        /// <summary>
        /// Which side the mod runs on. Not functional yet => No Client/Server only side mods
        /// </summary>
        GameType ExecutionSide { get; }

        /// <summary>
        /// PreLoad method
        /// </summary>
        void PreLoad();
        
        /// <summary>
        /// Main load method
        /// </summary>
        void Load();
        
        /// <summary>
        /// Post load method
        /// </summary>
        void PostLoad();
        
        /// <summary>
        /// Method to unload the mod. Free all unmanaged resources etc
        /// </summary>
        void Unload();
    }
}