using System;
using JetBrains.Annotations;

namespace MintyCore.Modding;

[PublicAPI]
public class ExternalDependency
{
    public ExternalDependency(string DependencyName, string DllName)
    {
        this.DependencyName = DependencyName;
        this.DllName = DllName;
    }

    public string DependencyName { get; set; }
    public string DllName { get; set; }

    public override bool Equals(object? obj)
    {
        if(obj is ExternalDependency dep)
            return DependencyName.Equals(dep.DependencyName) && DllName.Equals(dep.DllName);
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(DependencyName, DllName);
    }
}