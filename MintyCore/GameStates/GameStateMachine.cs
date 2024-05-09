using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Autofac;
using MintyCore.Modding;
using MintyCore.Network.Implementations;
using MintyCore.Utils;
using MintyCore.Utils.Events;
using Serilog;

namespace MintyCore.GameStates;

[Singleton<IGameStateMachine>]
internal class GameStateMachine(IModManager modManager, IEngineConfiguration engineConfiguration, IEventBus eventBus)
    : IGameStateMachine
{
    private readonly Dictionary<Identification, Action<ContainerBuilder>> _rootGameStateBuilders = new();
    private readonly Dictionary<Identification, Action<ContainerBuilder>> _modGameStateBuilders = new();
    private readonly Dictionary<Identification, Type> _initializationDataTypes = new();

    private (ILifetimeScope? Root, ILifetimeScope? Mods) _gameStateScopes = (null, null);

    private readonly Dictionary<Identification, GameState> _gameStates = new();

    private readonly Stack<Identification> _gameStateStack = new();
    private (Identification id, bool push, Action<GameState> initialize)? _nextGameState;
    private bool _popGameState;
    private bool _stop;

    private EventBinding<DisconnectedFromServerEvent>? _disconnectBinding;

    void IGameStateMachine.StartCore()
    {
        _disconnectBinding = new EventBinding<DisconnectedFromServerEvent>(OnDisconnectedFromServer);
        eventBus.AddListener(_disconnectBinding);

        while (true)
        {
            if (_stop)
            {
                CleanupCurrent(false);
                _gameStateStack.Clear();
                return;
            }

            CheckNextGameState();
            CheckPopGameState();

            if (!_gameStateStack.TryPeek(out var gameStateId)) return;

            var gameState = _gameStates[gameStateId];
            gameState.Update();
        }
    }

    private EventResult OnDisconnectedFromServer(DisconnectedFromServerEvent e)
    {
        _stop = true;
        return EventResult.Continue;
    }

    private void CheckNextGameState()
    {
        if (_nextGameState is not { } next) return;

        //Allow empty game stack, as the stack is obviously empty at the start of the game
        if (_gameStateStack.Count > 0)
        {
            CleanupCurrent(next.push);
        }

        _gameStateStack.Push(next.id);

        var nextState = _gameStates[next.id];
        next.initialize(nextState);

        _nextGameState = null;
    }

    private void CheckPopGameState()
    {
        if (!_popGameState) return;


        _popGameState = false;
    }

    private void CleanupCurrent(bool keepInStack)
    {
        var currentStateId = keepInStack switch
        {
            true => _gameStateStack.Peek(),
            false => _gameStateStack.Pop()
        };

        var currentState = _gameStates[currentStateId];
        currentState.Cleanup();
    }


    public void AddGameState<TGameState>(Identification id, GameStateDescription<TGameState> description)
        where TGameState : GameState
    {
        var builderAction = void (ContainerBuilder b) => b.RegisterType<TGameState>().Keyed<GameState>(id);

        switch (engineConfiguration.ModState)
        {
            case ModState.RootModsOnly:
                _rootGameStateBuilders.Add(id, builderAction);
                break;
            case ModState.AllMods:
                _modGameStateBuilders.Add(id, builderAction);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (TryFindInitializationDataType<TGameState>(out var type))
        {
            _initializationDataTypes.Add(id, type);
        }
    }

    private bool TryFindInitializationDataType<TGameState>([NotNullWhen(true)] out Type? type)
        where TGameState : GameState
    {
        var genericType = typeof(GameState<>);

        var currentType = typeof(TGameState);

        while (currentType is not null && currentType != typeof(object))
        {
            if (!currentType.IsGenericType || currentType.GetGenericTypeDefinition() != genericType)
            {
                currentType = currentType.BaseType;
                continue;
            }

            type = currentType.GetGenericArguments()[0];
            return true;
        }

        type = null;
        return false;
    }

    public void BuildNewGameStates()
    {
        var dic = engineConfiguration.ModState switch
        {
            ModState.RootModsOnly => _rootGameStateBuilders,
            ModState.AllMods => _modGameStateBuilders,
            _ => throw new ArgumentOutOfRangeException()
        };

        var lifetime = modManager.ModLifetimeScope.BeginLifetimeScope(builder =>
        {
            foreach (var builderAction in dic.Values)
            {
                builderAction(builder);
            }
        });

        foreach (var id in dic.Keys)
        {
            _gameStates[id] = lifetime.ResolveKeyed<GameState>(id);
        }

        switch (engineConfiguration.ModState)
        {
            case ModState.RootModsOnly:
                _gameStateScopes.Root = lifetime;
                break;
            case ModState.AllMods:
                _gameStateScopes.Mods = lifetime;
                break;
            case ModState.Invalid:
            case ModState.GameModsOnly:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void DestroyCurrentLifetimeScope()
    {
        switch (engineConfiguration.ModState)
        {
            case ModState.RootModsOnly:
            {
                _gameStateScopes.Root?.Dispose();
                _gameStateScopes.Root = null;
                break;
            }
            case ModState.AllMods:
            {
                _gameStateScopes.Mods?.Dispose();
                _gameStateScopes.Mods = null;
                break;
            }
            default: throw new ArgumentOutOfRangeException();
        }
    }

    public void RemoveGameState(Identification id)
    {
        switch (engineConfiguration.ModState)
        {
            case ModState.RootModsOnly:
                _rootGameStateBuilders.Remove(id);
                break;
            case ModState.AllMods:
                _modGameStateBuilders.Remove(id);
                break;
            default: throw new ArgumentOutOfRangeException();
        }

        _initializationDataTypes.Remove(id);
        _gameStates.Remove(id);
    }

    public void PushGameState(Identification id)
    {
        if (_gameStateStack.Contains(id))
        {
            Log.Error("Tried to push game state {@Id} which is already present in the game state stack", id);
        }

        if (_initializationDataTypes.ContainsKey(id))
            throw new InvalidOperationException(
                "Tried to push a game state without initialization parameter, which requires one");

        _nextGameState = (id, true, x => x.Initialize());
    }

    public void PushGameState<TInitializationParameter>(Identification id, TInitializationParameter parameter)
    {
        if (_gameStateStack.Contains(id))
        {
            Log.Error("Tried to push game state {@Id} which is already present in the game state stack", id);
        }

        if (!_initializationDataTypes.TryGetValue(id, out var initType))
            throw new InvalidOperationException(
                "Tried to push a game state with initialization parameter, which requires none");

        if (initType != typeof(TInitializationParameter))
            throw new InvalidOperationException("Tried to push a game state with the wrong initialization parameter");

        _nextGameState = (id, true, x => { (x as GameState<TInitializationParameter>)!.Initialize(parameter); });
    }

    public void ReplaceCurrentGameState(Identification id)
    {
        if (_gameStateStack.Contains(id) && _gameStateStack.Peek() != id)
        {
            Log.Error("Tried to set game state {@Id} which is already present in the game state stack", id);
        }

        if (_initializationDataTypes.ContainsKey(id))
            throw new InvalidOperationException(
                "Tried to set a game state without initialization parameter, which requires one");

        _nextGameState = (id, false, x => x.Initialize());
    }

    public void ReplaceCurrentGameState<TInitializationParameter>(Identification id, TInitializationParameter parameter)
    {
        if (_gameStateStack.Contains(id) && _gameStateStack.Peek() != id)
        {
            Log.Error("Tried to set game state {@Id} which is already present in the game state stack", id);
        }

        if (!_initializationDataTypes.TryGetValue(id, out var initType))
            throw new InvalidOperationException(
                "Tried to set a game state with initialization parameter, which requires none");

        if (initType != typeof(TInitializationParameter))
            throw new InvalidOperationException("Tried to set a game state with the wrong initialization parameter");

        _nextGameState = (id, false, x => { (x as GameState<TInitializationParameter>)!.Initialize(parameter); });
    }

    public void PopGameState()
    {
        _popGameState = true;
    }

    public void Stop()
    {
        _stop = true;
    }
}