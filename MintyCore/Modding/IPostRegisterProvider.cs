using Autofac.Core.Lifetime;

namespace MintyCore.Modding;

public interface IPostRegisterProvider
{
    void PostRegister(LifetimeScope scope);
}