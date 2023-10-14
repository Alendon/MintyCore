namespace MintyCore.Modding.Attributes;

public interface IRegistryProvider
{
    void Register(Autofac.ILifetimeScope lifetimeScope);
}