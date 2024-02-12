using Autofac;

namespace MintyCore.Modding.Providers;

/// <summary>
/// Helper interface used by source generation
/// </summary>
public interface IRegistryProvider
{
    /// <summary>
    /// </summary>
    void Register(ILifetimeScope lifetimeScope, ushort modId);
}