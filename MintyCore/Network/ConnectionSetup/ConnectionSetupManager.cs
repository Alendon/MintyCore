using System;
using System.Collections.Generic;
using Autofac;
using MintyCore.Utils;

namespace MintyCore.Network.ConnectionSetup;

[Singleton<IConnectionSetupManager>]
internal class ConnectionSetupManager : IConnectionSetupManager
{
    private readonly Dictionary<Identification, Action<ContainerBuilder>> _setupStateBuilders = new();
    
    
    public bool TryAddPendingConnection(int connectionId, DataReader connectionRequestData)
    {
        throw new System.NotImplementedException();
    }

    public bool RemovePendingConnection(int connectionId)
    {
        throw new System.NotImplementedException();
    }

    public DataWriter CreateConnectionRequest()
    {
        throw new System.NotImplementedException();
    }

    public void AddConnectionSetupState<TSetupState>(Identification setupState) where TSetupState : class, ISetupState
    {
        _setupStateBuilders.Add(setupState, builder =>
        {
            builder.RegisterType<TSetupState>()
                .Keyed<ISetupStateBase>(setupState)
                .ExternallyOwned();
        });
    }

    public void RemoveConnectionSetupState(Identification setupState)
    {
        DestroyDiContainer();
        _setupStateBuilders.Remove(setupState);
    }

    public TSetupState GetStateForConnection<TSetupState>(int connectionId) where TSetupState : ISetupStateBase
    {
        throw new System.NotImplementedException();
    }

    private void DestroyDiContainer();
    private void BuildDiContainer();
}