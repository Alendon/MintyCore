
using Autofac;

namespace MintyCore.Modding.Providers;

public interface IMainRegisterProvider
{
    void MainRegister(ILifetimeScope scope, ushort modId);
}