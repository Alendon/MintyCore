using Autofac;

namespace MintyCore.Modding;

public interface IPostRegisterProvider
{
    void PostRegister(ILifetimeScope scope, ushort modId);
}