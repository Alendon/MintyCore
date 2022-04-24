using System;
using JetBrains.Annotations;

namespace MintyCore.Modding.Attributes;

[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public class RegisterMethodAttribute : Attribute
{
    public RegisterMethodAttribute(ObjectRegistryPhase phase, RegisterMethodOptions options = RegisterMethodOptions.None)
    {
        
    }
}

[Flags]
public enum RegisterMethodOptions
{
    None = 0,
    HasFile = 1 << 0,
    UseExistingId = 1 << 1
}