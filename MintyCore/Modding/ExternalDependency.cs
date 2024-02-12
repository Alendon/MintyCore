using System;
using JetBrains.Annotations;

namespace MintyCore.Modding;

/// <summary>
/// Represents an external dependency for a mod.
/// </summary>
[PublicAPI]
public class ExternalDependency
{
    /// <summary>
    /// Initializes a new instance of the ExternalDependency class.
    /// </summary>
    /// <param name="dependencyName">The name of the dependency.</param>
    /// <param name="dllName">The name of the DLL file associated with the dependency.</param>
    public ExternalDependency(string dependencyName, string dllName)
    {
        DependencyName = dependencyName;
        DllName = dllName;
    }

    /// <summary>
    /// Gets or sets the name of the dependency.
    /// </summary>

    public string DependencyName { get; }

    /// <summary>
    /// Gets or sets the name of the DLL file associated with the dependency.
    /// </summary>
    public string DllName { get; }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is ExternalDependency dep)
            return DependencyName.Equals(dep.DependencyName) && DllName.Equals(dep.DllName);

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(DependencyName, DllName);
    }
}