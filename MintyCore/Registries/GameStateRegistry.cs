using System.Collections.Generic;
using System.Linq;
using MintyCore.GameStates;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("game_state")]
public class GameStateRegistry(IGameStateMachine gameStateMachine) : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.GameState;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();


    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterGameState<TGameState>(Identification id, GameStateDescription<TGameState> description)
        where TGameState : GameState
    {
        gameStateMachine.AddGameState(id, description);
    }

    public void PostRegister(ObjectRegistryPhase currentPhase)
    {
        if (currentPhase == ObjectRegistryPhase.Main)
            gameStateMachine.BuildNewGameStates();
    }

    public void PostUnRegister()
    {
        gameStateMachine.DestroyCurrentLifetimeScope();
    }

    public void UnRegister(Identification objectId)
    {
        gameStateMachine.RemoveGameState(objectId);
    }

    public void Clear()
    {
    }
}