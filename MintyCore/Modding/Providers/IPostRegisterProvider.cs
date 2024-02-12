using Autofac;

namespace MintyCore.Modding.Providers;

/// <summary>
/// Helper interface used by source generation
/// </summary>
public interface IPostRegisterProvider
{
    /// <summary>
    /// 
    /// </summary>
    void PostRegister(ILifetimeScope scope, ushort modId);
}