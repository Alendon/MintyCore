
using Autofac;

namespace MintyCore.Modding.Providers;

/// <summary>
/// Helper interface used by source generation
/// </summary>
public interface IMainRegisterProvider
{
    /// <summary>
    /// 
    /// </summary>
    void MainRegister(ILifetimeScope scope, ushort modId);
}