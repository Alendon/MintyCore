using System;

namespace MintyCore.Modding.Attributes;

public class RegistryObjectProviderAttribute : Attribute
{
    public string RegistryId { get; }
    public RegistryObjectProviderAttribute(string registryId)
    {
        RegistryId = registryId;
    }
}