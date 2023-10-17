using Autofac;

namespace MintyCore.Modding.Providers;

public interface IPostRegisterProvider
{
    void PostRegister(ILifetimeScope scope, ushort modId);
}