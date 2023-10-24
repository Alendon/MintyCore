using Autofac;

namespace MintyCore.Modding.Providers;

public interface IRegistryProvider
{
    void Register(ILifetimeScope lifetimeScope, ushort modId);
}