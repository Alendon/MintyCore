using MintyCore.Utils;

namespace MintyCore.Network.ConnectionSetup;

/// <summary>
/// Represents a state in the connection setup process
/// Each state should be responsible for a specific task
/// Each connecting client gets a unique instance of each state
/// </summary>
public interface ISetupStateBase
{
    /// <summary>
    /// The id of the connection that is being set up
    /// 0 if the setup is executed on the client side
    /// </summary>
    int ConnectionId { set; }
    
    /// <summary>
    /// Indicates if the state is executed on the server side
    /// </summary>
    bool IsServerSide { set; }

    /// <summary>
    /// Starts the state
    /// </summary>
    void Start();

    /// <summary>
    /// Processes the state
    /// Called every frame
    /// </summary>
    void Process();

    /// <summary>
    ///  Indicates if the state has finished
    /// </summary>
    bool IsFinished { get; }
}

public interface ISetupState : ISetupStateBase
{
    /// <summary>
    /// Defines the states that should be executed before this state
    /// </summary>
    static abstract Identification[] ExecuteAfter { get; }

    /// <summary>
    /// Defines the states that should be executed after this state
    /// </summary>
    static abstract Identification[] ExecuteBefore { get; }

    /// <summary>
    /// Defines if other states can be executed at the same time as this state
    /// </summary>
    static abstract bool Exclusive { get; }
}