using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace MintyCore.Modding;

internal class SharedAssemblyLoadContext : AssemblyLoadContext
{
    private static readonly Dictionary<string, WeakReference<Assembly>> _sharedAssemblies = new();

    private List<string> _loadedAssemblies = new();
    private static bool _isInitialized;

    public SharedAssemblyLoadContext() : base(true)
    {
        if (_isInitialized) return;

        Unloading += OnUnloading;
        _isInitialized = true;
    }

    private static void OnUnloading(AssemblyLoadContext obj)
    {
        if (obj is not SharedAssemblyLoadContext sharedLoadContext) return;

        foreach (var assembly in sharedLoadContext._loadedAssemblies)
        {
            _sharedAssemblies.Remove(assembly);
        }
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var baseLoad = base.Load(assemblyName);

        if (baseLoad is not null)
            return baseLoad;

        if (assemblyName.Name is null) return null;

        if (_sharedAssemblies.TryGetValue(assemblyName.Name, out var reference) &&
            reference.TryGetTarget(out var assembly))
        {
            return assembly;
        }

        if (!ModManager.GetAssemblyStream(assemblyName.Name, out Stream assemblyStream)) return null;
        
        assembly = LoadFromStream(assemblyStream);
        _sharedAssemblies[assemblyName.Name] = new WeakReference<Assembly>(assembly);
        _loadedAssemblies.Add(assemblyName.Name);
        
        return assembly;

    }
    
}