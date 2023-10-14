using Autofac;

namespace MintyCore.Modding;

public interface IPreRegisterProvider
{
    void PreRegister(ILifetimeScope scope, ushort modId);
}