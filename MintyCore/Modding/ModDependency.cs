using System;
using JetBrains.Annotations;

namespace MintyCore.Modding;

/// <summary>
///     Struct to represent a mod dependency
/// </summary>
[PublicAPI]
public readonly struct ModDependency
{
    /// <summary>
    ///     String identifier of the dependent mod
    /// </summary>
    public readonly string StringIdentifier;

    /// <summary>
    ///     Version of the dependency
    /// </summary>
    public readonly Version ModVersion;

    /// <summary>
    ///     Create a new dependency
    /// </summary>
    public ModDependency(string stringIdentifier, Version modVersion)
    {
        StringIdentifier = stringIdentifier;
        ModVersion = modVersion;
    }
}