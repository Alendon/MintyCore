using System;
using System.Collections.Generic;
using Autofac;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers.Implementations;

[Singleton<IInputModuleManager>(SingletonContextFlags.NoHeadless)]
internal class InputModuleManager : IInputModuleManager
{
    private readonly Dictionary<Identification, Action<ContainerBuilder, Identification>> _registeredInputDataModules =
        new();

    private readonly HashSet<Identification> _activeModules = new();

    public IReadOnlySet<Identification> RegisteredInputModuleIds =>
        new HashSet<Identification>(_registeredInputDataModules.Keys);


    public Dictionary<Identification, InputModule> CreateInputModuleInstances(out IContainer container)
    {
        var builder = new ContainerBuilder();

        foreach (var id in _activeModules)
            _registeredInputDataModules[id](builder, id);

        container = builder.Build();

        var instances = new Dictionary<Identification, InputModule>();

        foreach (var id in _activeModules)
            instances.Add(id, container.ResolveKeyed<InputModule>(id));

        return instances;
    }

    public void SetModuleActive(Identification moduleTestId, bool isActive)
    {
        if (isActive)
            _activeModules.Add(moduleTestId);
        else
            _activeModules.Remove(moduleTestId);
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