using JetBrains.Annotations;

namespace MintyCore.Modding;

[PublicAPI]
internal class ExternalDependency
{
    public string DependencyName { get; internal set; }
    public string DllName { get; internal set; }

    public ExternalDependency(string dependencyName, string dllName)
    {
        DependencyName = dependencyName;
        DllName = dllName;
    }
}