namespace MintyCore.Modding.Providers;

public interface IRegistryProvider
{
    void Register(Autofac.ILifetimeScope lifetimeScope, ushort modId);
}