using MintyCore.Utils;

namespace MintyCore.Network.ConnectionSetup;

public interface IConnectionSetupManager
{
    bool TryAddPendingConnection(int connectionId, DataReader connectionRequestData);
    bool RemovePendingConnection(int connectionId);
    
    DataWriter CreateConnectionRequest();


    void AddConnectionSetupState<TSetupState>(Identification setupState) where TSetupState : class, ISetupState;
    void RemoveConnectionSetupState(Identification setupState);
    
    TSetupState GetStateForConnection<TSetupState>(int connectionId) where TSetupState : ISetupStateBase;
}