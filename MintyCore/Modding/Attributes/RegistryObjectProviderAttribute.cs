using System;

namespace MintyCore.Modding.Attributes;

/// <summary>
/// Helper attribute to be used for source generation
/// </summary>
public class RegistryObjectProviderAttribute : Attribute
{
    /// <summary/>
    public string RegistryId { get; }

    /// <summary/>
    public RegistryObjectProviderAttribute(string registryId)
    {
        RegistryId = registryId;
    }
}