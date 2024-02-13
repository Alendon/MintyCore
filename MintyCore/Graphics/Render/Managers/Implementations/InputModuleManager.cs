using System;
using System.Collections.Generic;
using Autofac;
using MintyCore.Modding;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers.Implementations;

[Singleton<IInputModuleManager>(SingletonContextFlags.NoHeadless)]
internal class InputModuleManager(IModManager modManager) : IInputModuleManager
{
    private readonly Dictionary<Identification, Action<ContainerBuilder, Identification>> _registeredInputDataModules =
        new();

    private readonly HashSet<Identification> _activeModules = new();

    public IReadOnlySet<Identification> RegisteredInputModuleIds =>
        new HashSet<Identification>(_registeredInputDataModules.Keys);


    public Dictionary<Identification, InputModule> CreateInputModuleInstances(out ILifetimeScope lifetimeScope)
    {
        lifetimeScope = modManager.ModLifetimeScope.BeginLifetimeScope(
            builder =>
            {
                foreach (var moduleId in _activeModules)
                {
                    _registeredInputDataModules[moduleId](builder, moduleId);
                }
            }
        );

        var instances = new Dictionary<Identification, InputModule>();
        foreach (var moduleId in _activeModules)
        {
            instances.Add(moduleId, lifetimeScope.ResolveKeyed<InputModule>(moduleId));
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

    public void UnRegisterInputModule(Identification objectId)
    {
        _activeModules.Remove(objectId);
        _registeredInputDataModules.Remove(objectId);
    }

    public void Clear()
    {
        _activeModules.Clear();
        _registeredInputDataModules.Clear();
    }

    public void RegisterInputModule<TModule>(Identification id) where TModule : InputModule
    {
        if (!_registeredInputDataModules.TryAdd(id, BuilderAction))
            throw new MintyCoreException($"Input Data Module for {id} is already registered");

        _activeModules.Add(id);
        return;

        static void BuilderAction(ContainerBuilder cb, Identification id) =>
            cb.RegisterType<TModule>().Keyed<InputModule>(id);
    }
}