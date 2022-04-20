using System;

namespace MintyCore.Modding.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class RegisterMethodAttribute : Attribute
{
    public RegisterMethodAttribute(ObjectRegistryPhase phase, bool hasFile = false)
    {
        
    }
}