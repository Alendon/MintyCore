using System.Linq;
using System.Reflection;
using Autofac;

namespace MintyCore.Utils;

public static class AutofacHelper
{
    public const string UnsafeSelfName = "unsafe-self";

    public static ContainerBuilder RegisterMarkedSingletons(this ContainerBuilder builder, Assembly assembly,
        SingletonContextFlags contextFlags)
    {
        var types = assembly.GetTypes();
        foreach (var type in types)
        {
            var attributes = type.GetCustomAttributes(typeof(BaseSingletonAttribute)).ToArray();
            if (attributes.Length == 0) continue;

            var registration = builder.RegisterType(type).Named(UnsafeSelfName, type).SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            foreach (var attribute in attributes)
            {
                if (attribute is not BaseSingletonAttribute baseAttribute) continue;

                if ((baseAttribute.ContextFlags & contextFlags) != baseAttribute.ContextFlags) continue;

                registration.As(baseAttribute.ImplementedType);
            }
        }

        return builder;
    }
}