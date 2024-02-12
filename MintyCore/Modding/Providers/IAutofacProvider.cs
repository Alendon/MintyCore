using Autofac;
using MintyCore.Modding.Attributes;

namespace MintyCore.Modding.Providers;

/// <summary>
/// Interface to allow custom autofac registrations
/// Implement this interface in your mod assembly and it will be used for the mod container creation
/// Annotate the implementing class with <see cref="AutofacProviderAttribute"/> to make it discoverable
/// </summary>
public interface IAutofacProvider
{
    /// <summary>
    ///  Register your types here
    /// </summary>
    /// <param name="builder"> The <see cref="ContainerBuilder"/> to register your types to </param>
    public void Register(ContainerBuilder builder);
}