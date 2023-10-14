
using Autofac;

namespace MintyCore.Modding;

public interface IMainRegisterProvider
{
    void MainRegister(ILifetimeScope scope, ushort modId);
}