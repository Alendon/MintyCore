using Autofac.Core.Lifetime;

namespace MintyCore.Modding;

public interface IPreRegisterProvider
{
    void PreRegister(LifetimeScope scope);
}