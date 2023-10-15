using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace MintyCore.Modding;

public class SharedAssemblyLoadContext : AssemblyLoadContext
{
    private static readonly Dictionary<string, WeakReference<Assembly>> _sharedAssemblies = new();
    private static readonly Dictionary<string, GCHandle> _sharedMetadata = new();

    private readonly List<string> _loadedAssemblies = new();
    
    public SharedAssemblyLoadContext() : base(true)
    {
    }

    internal static bool TryGetMetadata(string name, [MaybeNullWhen(false)] out Metadata metadata)
    {
        if (_sharedMetadata.TryGetValue(name, out var weakRef) && weakRef.Target is Metadata moduleMetadata)
        {
            metadata = moduleMetadata;
            return true;
        }

        metadata = null;
        return false;
    }

    public new void Unload()
    {
        OnUnloading();
        base.Unload();
    }
    
    private void OnUnloading()
    {
        foreach (var assembly in _loadedAssemblies)
        {
            _sharedAssemblies.Remove(assembly);
            if (_sharedMetadata.Remove(assembly, out var handle))
            {
                handle.Free();
            }
        }
        
        _loadedAssemblies.Clear();
    }

    public Assembly CustomLoadFromStream(Stream dllStream, Stream? pdbStream = null)
    {
        var result = pdbStream is not null ? LoadFromStream(dllStream, pdbStream) : LoadFromStream(dllStream);

        var assemblyName = result.GetName();

        if (_sharedAssemblies.TryGetValue(assemblyName.FullName, out var weakReference))
        {
            if (weakReference.TryGetTarget(out var target))
            {
                return target;
            }
        }

        dllStream.Seek(0, SeekOrigin.Begin);
        var baseModule = ModuleMetadata.CreateFromStream(dllStream, PEStreamOptions.LeaveOpen);

        if (baseModule.GetModuleNames().Length != 0)
        {
            //investigate what happens when a assembly has multiple modules
            //for now we just throw an exception and hope that it never happens
            throw new NotSupportedException();
        }

        _sharedMetadata.Add(result.GetName().FullName,
            GCHandle.Alloc(AssemblyMetadata.Create(baseModule), GCHandleType.Normal));

        _sharedAssemblies.Add(result.GetName().FullName, new WeakReference<Assembly>(result));
        _loadedAssemblies.Add(result.GetName().FullName);
        return result;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is "MintyCore")
            return GetType().Assembly;

        if (assemblyName.Name is not null &&
            _sharedAssemblies.TryGetValue(assemblyName.FullName, out var reference) &&
            reference.TryGetTarget(out var assembly))
        {
            return assembly;
        }

        return base.Load(assemblyName);
    }
}