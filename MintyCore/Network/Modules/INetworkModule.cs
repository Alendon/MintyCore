using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MintyCore.Utils;
using OneOf;
using OneOf.Types;

namespace MintyCore.Network.Modules;

public interface INetworkModuleBase
{
    /// <summary>
    /// The connection of the network module.
    /// Is either a player or a temporary connection id for pending connections.
    /// </summary>
    public OneOf<Player, int> Connection { get; set; }
    
    public Task<OneOf<Success, Error<string>>> Initialize();
    public Task<OneOf<Success, Error<string>>> Shutdown();
}

public interface INetworkModule : INetworkModuleBase
{
    public static abstract IEnumerable<Identification> MessageCategories { get; }
    public static abstract IEnumerable<Identification> NetworkModuleDependencies { get; }
    public static abstract int Priority { get; }
    
    /// <summary>
    /// The amount of dedicated channels for this network module.
    /// </summary>
    public static abstract int DedicatedChannelCount { get; }
}