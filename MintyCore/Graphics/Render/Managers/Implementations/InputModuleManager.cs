using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Builder;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers.Implementations;

[Singleton<IInputModuleManager>(SingletonContextFlags.NoHeadless)]
internal class InputModuleManager : IInputModuleManager
{
    private readonly Dictionary<Identification, Action<ContainerBuilder, Identification>> _registeredInputDataModules = new();
    private readonly HashSet<Identification> _activeInputModules = new();

    public IReadOnlySet<Identification> RegisteredInputModuleIds =>
        new HashSet<Identification>(_registeredInputDataModules.Keys);

    public IManualAsyncWorker CreateInputWorker()
    {
        throw new System.NotImplementedException();
    }

    public void RegisterInputModule<TModule>(Identification id) where TModule : InputModule
    {
        static void BuilderAction(ContainerBuilder cb, Identification id) => cb.RegisterType<TModule>().Keyed<InputModule>(id);

        if (!_registeredInputDataModules.TryAdd(id, BuilderAction))
            throw new MintyCoreException($"Input Data Module for {id} is already registered");

        _activeInputModules.Add(id);
    }
}