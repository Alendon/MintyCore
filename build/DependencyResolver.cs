using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Nuke.Common;

class DependencyResolver
{
    readonly JsonDocument DependencyTree;
    readonly string Mod;
    readonly List<string> ModDependencies;
    readonly JsonElement DependencyRoot;

    public DependencyResolver(JsonDocument dependencyTree, string mod, List<string> modDependencies)
    {
        DependencyTree = dependencyTree;
        Mod = mod;
        ModDependencies = modDependencies;
        DependencyRoot = GetDependencyRoot();
    }


    public List<Dependency> GetDependencies()
    {
        var rootDependency = GetDependencyDetails(Mod);
        var toProcess = new Queue<string>();
        var dependencies = new List<Dependency>();

        AddDependenciesToProcess(rootDependency.dependencies);

        while (toProcess.TryDequeue(out var dependencyName))
        {
            var depDetails = GetDependencyDetails(dependencyName);

            AddDependenciesToProcess(depDetails.dependencies);

            if (depDetails.dll is not null)
                dependencies.Add(new Dependency(dependencyName, depDetails.dll));
        }


        return dependencies;

        void AddDependenciesToProcess(IEnumerable<string> dependencies)
        {
            foreach (var dependency in dependencies)
            {
                //System dlls can be ignored
                if (dependency.StartsWith("System."))
                    continue;

                //Dependencies to other mods are handled by the mod manager
                if (ModDependencies.Any(x => x.Equals(dependency)))
                    continue;

                toProcess.Enqueue(dependency);
            }
        }
    }

    JsonElement GetDependencyRoot()
    {
        var targets = DependencyTree.RootElement.GetProperty("targets");
        var targetArray = targets.EnumerateObject().ToArray();
        Assert.Count(targets.EnumerateObject().ToArray(), 1, "Dependency tree should only have one target");
        return targetArray[0].Value;
    }

    (string[] dependencies, string? dll) GetDependencyDetails(string name)
    {
        var element = DependencyRoot.EnumerateObject().First(x => x.Name.StartsWith($"{name}/")).Value;

        string[] dependencies = Array.Empty<string>();
        //Get dependencies of the current dependency
        if (element.TryGetProperty("dependencies", out var dependencyTree))
        {
            dependencies = dependencyTree.EnumerateObject().Select(x => x.Name).ToArray();
        }

        string? dll = null;
        //Get the dlls of the current dependency. If there are no dlls, fine
        if (element.TryGetProperty("runtime", out var runtime))
        {
            dll = runtime.EnumerateObject().Select(x => x.Name.Split("/").Last()).FirstOrDefault();
        }

        return (dependencies, dll);
    }
}

class Dependency
{
    public string DependencyName { get; set; }
    public string DllName { get; set; }

    public Dependency(string dependencyName, string dllName)
    {
        DependencyName = dependencyName;
        DllName = dllName;
    }
}