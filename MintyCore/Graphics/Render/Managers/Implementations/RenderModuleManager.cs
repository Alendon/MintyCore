using System;
using System.Collections.Generic;
using Autofac;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers.Implementations;

[Singleton<IRenderModuleManager>(SingletonContextFlags.NoHeadless)]
public class RenderModuleManager(ILifetimeScope parentScope) : IRenderModuleManager
{
    private readonly Dictionary<Identification, Action<ContainerBuilder, Identification>> _registeredRenderModules =
        new();

    private readonly HashSet<Identification> _activeModules = new();
    
    public void RegisterRenderModule<TRenderModule>(Identification identification) where TRenderModule : RenderModule
    {
        if (_registeredRenderModules.ContainsKey(identification))
            throw new InvalidOperationException($"Render module with id {identification} already registered");

        _registeredRenderModules.Add(identification,
            (builder, id) => builder.RegisterType<TRenderModule>().Keyed<RenderModule>(id));
    }

    public IReadOnlySet<Identification> RegisteredRenderModuleIds =>
        new HashSet<Identification>(_registeredRenderModules.Keys);

    public Dictionary<Identification, RenderModule> CreateRenderModuleInstances(out ILifetimeScope lifetimeScope)
    {
        lifetimeScope = parentScope.BeginLifetimeScope(
            builder =>
            {
                foreach (var moduleId in _activeModules)
                {
                    _registeredRenderModules[moduleId](builder, moduleId);
                }
            }
        );
        
        var instances = new Dictionary<Identification, RenderModule>();
        
        foreach (var moduleId in _activeModules)
        {
            instances.Add(moduleId, lifetimeScope.ResolveKeyed<RenderModule>(moduleId));
        }
        
        return instances;
    }

    public void SetModuleActive(Identification moduleId, bool isActive)
    {
        if (isActive)
            _activeModules.Add(moduleId);
        else
            _activeModules.Remove(moduleId);
    }

    public bool IsModuleActive(Identification moduleId)
    {
        return _activeModules.Contains(moduleId);
    }

    public void UnRegisterRenderModule(Identification objectId)
    {
        _activeModules.Remove(objectId);
        _registeredRenderModules.Remove(objectId);
    }

    public void Clear()
    {
        _activeModules.Clear();
        _registeredRenderModules.Clear();
    }
}