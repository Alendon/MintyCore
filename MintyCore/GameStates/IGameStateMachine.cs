using JetBrains.Annotations;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.GameStates;

/// <summary>
///   Interface for the game state machine
/// </summary>
public interface IGameStateMachine
{
    /// <summary>
    /// Add a game state to the state machine
    /// </summary>
    /// <param name="id"> The identification of the game state </param>
    /// <param name="description"> The description of the game state </param>
    /// <typeparam name="TGameState"> The type of the game state </typeparam>
    /// <remarks>This Method is intended to be used by the <see cref="GameStateRegistry"/></remarks>
    void AddGameState<TGameState>(Identification id, GameStateDescription<TGameState> description)
        where TGameState : GameState;

    void BuildNewGameStates();
    void DestroyCurrentLifetimeScope();

    /// <summary>
    /// Remove a game state from the state machine
    /// </summary>
    /// <param name="id"> The identification of the game state </param>
    /// <remarks> This Method is intended to be used by the <see cref="GameStateRegistry"/></remarks>
    void RemoveGameState(Identification id);

    /// <summary>
    ///  Push a game state to the state machine
    /// </summary>
    /// <param name="id"> The identification of the game state </param>
    void PushGameState(Identification id);

    /// <summary>
    ///  Push a game state with initialization data to the state machine
    /// </summary>
    /// <param name="id"> The identification of the game state </param>
    /// <param name="parameter"> The initialization data </param>
    /// <typeparam name="TInitializationParameter"> The type of the initialization data </typeparam>
    void PushGameState<TInitializationParameter>(Identification id, TInitializationParameter parameter);

    /// <summary>
    ///  Replace the current game state with a new one
    /// </summary>
    /// <param name="id"> The identification of the game state </param>
    void ReplaceCurrentGameState(Identification id);

    /// <summary>
    ///  Replace the current game state with a new one with initialization data
    /// </summary>
    /// <param name="id"> The identification of the game state </param>
    /// <param name="parameter"> The initialization data </param>
    /// <typeparam name="TInitializationParameter"> The type of the initialization data </typeparam>
    void ReplaceCurrentGameState<TInitializationParameter>(Identification id, TInitializationParameter parameter);

    /// <summary>
    ///  Pop the current game state
    /// </summary>
    void PopGameState();

    /// <summary>
    /// Start the state machine
    /// </summary>
    internal sealed void Start()
    {
        StartCore();
    }

    /// <summary>
    /// Start the game state machine
    /// </summary>
    protected void StartCore();

    /// <summary>
    /// Stop the game state machine
    /// This will close the game!
    /// </summary>
    void Stop();
}

public record GameStateDescription<[UsedImplicitly] TGameState> where TGameState : GameState;