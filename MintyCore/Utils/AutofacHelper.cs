using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;

namespace MintyCore.Utils;

/// <summary>
/// Helper class for Autofac
/// </summary>
public static class AutofacHelper
{
    /// <summary>
    /// Name used to register the type with itself
    /// </summary>
    public const string UnsafeSelfName = "unsafe-self";

    /// <summary>
    ///   Register all types marked with a <see cref="BaseSingletonAttribute" /> as a singleton
    /// </summary>
    /// <param name="builder"> The <see cref="ContainerBuilder" /> to register the types with </param>
    /// <param name="assembly"> The assembly to search for types in </param>
    /// <param name="contextFlags"> The context flags to register the types with </param>
    public static ContainerBuilder RegisterMarkedSingletons(this ContainerBuilder builder, Assembly assembly,
        SingletonContextFlags contextFlags)
    {
        var types = assembly.GetTypes();
        foreach (var type in types)
        {
            var attributes = type.GetCustomAttributes(typeof(BaseSingletonAttribute)).ToArray();
            if (attributes.Length == 0) continue;
            
            IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle>? registration = null;

            foreach (var attribute in attributes)
            {
                if (attribute is not BaseSingletonAttribute baseAttribute) continue;

                if ((baseAttribute.ContextFlags & contextFlags) != baseAttribute.ContextFlags) continue;

                registration ??= builder.RegisterType(type).Named(UnsafeSelfName, type).SingleInstance()
                    .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

                registration.As(baseAttribute.ImplementedType);
            }
        }

        return builder;
    }
}