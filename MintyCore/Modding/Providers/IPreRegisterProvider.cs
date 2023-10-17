using Autofac;

namespace MintyCore.Modding.Providers;

public interface IPreRegisterProvider
{
    void PreRegister(ILifetimeScope scope, ushort modId);
}