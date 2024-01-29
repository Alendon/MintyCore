using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Implementations;

[Singleton<IInputModuleManager>(SingletonContextFlags.NoHeadless)]
internal class InputModuleManager : IInputModuleManager
{
    private readonly HashSet<Identification> _registeredInputDataModules = new();
    
    public IReadOnlySet<Identification> RegisteredInputModuleIds => _registeredInputDataModules;
    public IManualAsyncWorker CreateInputWorker()
    {
        throw new System.NotImplementedException();
    }

    public void RegisterInputModule<TModule>(Identification id) where TModule : InputDataModule
    {
        if (!_registeredInputDataModules.Add(id))
            throw new MintyCoreException($"Input Data Module for {id} is already registered");

        _registeredInputDataModules.Add(id);
    }
}