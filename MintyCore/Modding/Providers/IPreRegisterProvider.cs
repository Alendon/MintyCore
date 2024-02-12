using Autofac;

namespace MintyCore.Modding.Providers;
/// <summary>
/// Helper interface used by source generation
/// </summary>
public interface IPreRegisterProvider
{
    /// <summary>
    /// 
    /// </summary>
    void PreRegister(ILifetimeScope scope, ushort modId);
}