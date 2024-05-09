using System;

namespace MintyCore.GameStates;

/// <summary>
/// Base class for game states
/// </summary>
public abstract class GameState
{
    public abstract void Initialize();
    public abstract void Update();
    public abstract void Cleanup();
}

/// <summary>
/// Base class for game states with initialization data
/// </summary>
/// <typeparam name="TInitializationData">The type of the initialization data</typeparam>
public abstract class GameState<TInitializationData> : GameState
{
    public abstract void Initialize(TInitializationData data);
    
    public sealed override void Initialize()
    {
        throw new InvalidOperationException("This game state requires initialization data");
    }
}